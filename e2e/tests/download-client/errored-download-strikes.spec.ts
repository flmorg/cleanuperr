import { existsSync, readdirSync, rmSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { test, expect, TEST_CONFIG } from '../fixtures/base';
import { CleanuparrApi } from '../helpers/api';
import { buildDownloadClientPayload, DownloadClientType } from '../helpers/api/download-client';
import { ArrStubs, NotificationStubs } from '../helpers/mocks';
import { MockServers } from '../helpers/mocks/wiremock-client';
import { QBittorrentDriver } from '../helpers/torrent-clients/qbittorrent';
import { TransmissionDriver } from '../helpers/torrent-clients/transmission';
import { buildFolderTorrent } from '../helpers/torrent-fixtures';
import { appLogsSince, restartComposeService } from '../helpers/test-lifecycle';

/**
 * The Queue Cleaner's errored-download check, against real clients.
 *
 * Covers what no unit test can: that the resume call Cleanuparr issues is one the client's
 * API actually accepts, and that an *arr reporting the item as healthy no longer hides it.
 * Sonarr maps a client error to a warning with `status: ok` and `trackedDownloadState:
 * downloading`, which is why the *arr-side checks never fired on these downloads.
 *
 * Reaching a real error state differs per client, measured against these images:
 *  - Transmission 4.0.6 sets `error: 3` ("No data found!") as soon as a verify runs, and a
 *    start leaves it stopped while the data is still gone. The error persists, so this is
 *    the client that can reach the strike limit.
 *  - qBittorrent 5.2.3 reports `missingFiles` only after reloading resume data, so the spec
 *    restarts the container. A resume then makes it re-download, clearing the error, so it
 *    is covered for detection and resume but not for the limit.
 *
 * Deluge and uTorrent have no reliable route: both treat missing data as a reason to
 * re-download, and an unwritable save path raises nothing until a write is attempted,
 * which needs a seeding peer the suite does not have. Their predicates are covered by the
 * wrapper unit tests instead.
 */

const HOST_DOWNLOADS = resolve(__dirname, '..', '..', 'test-data', 'downloads');
const QBIT_RESUME_DIR = resolve(__dirname, '..', '..', 'test-data', 'qbittorrent-config', 'qBittorrent', 'BT_backup');
const CLIENT_DOWNLOADS = '/downloads';
const MAX_STRIKES = 3;
const SERIES_ID = 1;
const EPISODE_ID = 2;
const QUEUE_ITEM_ID = 7001;
const RESUME_FAILURE_LOG = 'failed to resume errored download';

const qbit = new QBittorrentDriver();
const transmission = new TransmissionDriver();

/**
 * Stubs the *arr queue the way Sonarr reports a torrent its client has errored:
 * `status: ok` with `trackedDownloadState: downloading`.
 */
async function stubHealthyLookingQueue(mocks: MockServers, downloadId: string): Promise<void> {
  await ArrStubs.applyArrDefaults(mocks.arr);
  await mocks.arr.stub(
    ArrStubs.arrRawQueueStub(
      JSON.stringify({
        page: 1,
        pageSize: 50,
        totalRecords: 1,
        records: [
          {
            id: QUEUE_ITEM_ID,
            // Without a content id the Queue Cleaner skips the item before any check runs.
            seriesId: SERIES_ID,
            episodeId: EPISODE_ID,
            title: 'errored.test.s01e01',
            status: 'ok',
            trackedDownloadStatus: 'ok',
            trackedDownloadState: 'downloading',
            downloadId,
            protocol: 'torrent',
            size: 32768,
            sizeleft: 32768,
          },
        ],
      }),
    ),
  );
}

async function countEvents(api: CleanuparrApi, eventType: string, downloadId: string): Promise<number> {
  const res = await api.events.list({ eventType, page: 1, pageSize: 500 });
  expect(res.status, 'events query failed').toBe(200);

  const body: { items?: Array<{ itemHash?: string }> } = await res.json();

  return (body.items ?? []).filter((e) => (e.itemHash ?? '').toLowerCase() === downloadId.toLowerCase()).length;
}

interface Arrangement {
  clientId: string;
  arrInstanceId: string;
  savedQueueCleanerConfig: Record<string, unknown>;
}

interface ErroredSettings {
  ignorePrivate?: boolean;
  deletePrivate?: boolean;
  changeCategory?: boolean;
}

/** Registers the client and an *arr, then turns the errored check on. */
async function arrangeCleanuparr(
  api: CleanuparrApi,
  clientType: DownloadClientType,
  host: string,
  username: string,
  password: string,
  errored: ErroredSettings = {},
): Promise<Arrangement> {
  const created = await api.downloadClient.create(
    buildDownloadClientPayload(clientType, {
      name: `${clientType}-errored-${Date.now()}`,
      host,
      username,
      password,
    }),
  );
  const createdBody = await created.text();
  expect(created.status, createdBody).toBeLessThan(300);

  const arr = await api.arr.createInstance('sonarr', {
    name: `sonarr-errored-${Date.now()}`,
    url: TEST_CONFIG.mocks.arrUrl,
    apiKey: 'k',
    version: 3,
    enabled: true,
  });
  const arrBody = await arr.text();
  expect(arr.status, arrBody).toBeLessThan(300);

  const savedQueueCleanerConfig = await api.queueCleaner.getJsonConfig();

  // patch() merges one level deep, so the whole nested object has to go over.
  await api.queueCleaner.patch({
    errored: {
      maxStrikes: MAX_STRIKES,
      ignorePrivate: errored.ignorePrivate ?? false,
      deletePrivate: errored.deletePrivate ?? false,
      changeCategory: errored.changeCategory ?? false,
    },
  });

  return {
    clientId: JSON.parse(createdBody).id as string,
    arrInstanceId: JSON.parse(arrBody).id as string,
    savedQueueCleanerConfig,
  };
}

async function restore(api: CleanuparrApi, arranged: Arrangement | undefined): Promise<void> {
  if (!arranged) {
    return;
  }
  await api.queueCleaner.updateConfig(arranged.savedQueueCleanerConfig);
  await api.arr.deleteInstance('sonarr', arranged.arrInstanceId);
  await api.downloadClient.delete(arranged.clientId);
}

/**
 * Whether qBittorrent has written the torrent's resume data.
 *
 * The container restart below only yields `missingFiles` when qBittorrent reloads resume data
 * that says the torrent was complete. Deleting the files before that write leaves it
 * rechecking into `stalledDL` instead, which made this spec flaky when it waited on the
 * in-memory state alone.
 */
function hasPersistedResumeData(infoHash: string): boolean {
  if (!existsSync(QBIT_RESUME_DIR)) {
    return false;
  }
  const hash = infoHash.toLowerCase();
  return readdirSync(QBIT_RESUME_DIR).some((f) => f.toLowerCase().startsWith(hash) && f.endsWith('.fastresume'));
}

function queueDeletes(mocks: MockServers): Promise<Array<{ url: string }>> {
  return mocks.arr.findRequests({ method: 'DELETE', urlPattern: '/api/v3/queue/.*' });
}

/** Count of GET /api/v3/queue calls so far, the request every QueueCleaner run makes to fetch the queue. */
async function queueGetCount(mocks: MockServers): Promise<number> {
  return (await mocks.arr.findRequests({ method: 'GET', urlPath: '/api/v3/queue' })).length;
}

/**
 * Drives a Transmission torrent into its local-error state and stubs the *arr queue for it.
 *
 * Every fixture torrent is private, which is what makes it useful here: the private settings
 * below only bite on a private download.
 */
async function arrangeErroredTransmissionTorrent(
  mocks: MockServers,
  label: string,
): Promise<{ infoHash: string }> {
  await transmission.ready();
  await transmission.clearAllTorrents();

  const torrent = buildFolderTorrent(join(HOST_DOWNLOADS, 'transmission'), `${label}-${Date.now()}`);
  await transmission.addSeedingTorrent({
    metainfo: torrent.metainfo,
    savePath: CLIENT_DOWNLOADS,
    category: '',
    infoHash: torrent.infoHash,
  });

  rmSync(torrent.contentPath, { recursive: true, force: true });
  await transmission.verifyTorrent(torrent.infoHash);

  await expect
    .poll(async () => (await transmission.getError(torrent.infoHash)).error, {
      message: 'Transmission never reported a local error',
      timeout: 60_000,
    })
    .toBe(3);

  await stubHealthyLookingQueue(mocks, torrent.infoHash);

  return { infoHash: torrent.infoHash };
}

/**
 * Triggers one Queue Cleaner run and waits until it queries the *arr queue.
 *
 * The trigger endpoint only schedules the Quartz job. The job disallows concurrent execution,
 * so a run that has started also proves the previous run finished.
 */
async function runQueueCleaner(api: CleanuparrApi, mocks: MockServers): Promise<void> {
  const before = await queueGetCount(mocks);
  expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

  await expect
    .poll(() => queueGetCount(mocks), {
      message: 'QueueCleaner run never queried the *arr queue',
      timeout: 60_000,
    })
    .toBeGreaterThan(before);
}

/** Runs the Queue Cleaner enough times to reach the strike limit. */
async function strikeToTheLimit(api: CleanuparrApi, mocks: MockServers): Promise<void> {
  for (let run = 0; run < MAX_STRIKES; run++) {
    await runQueueCleaner(api, mocks);
  }
}

test.describe('Queue Cleaner errored downloads', () => {
  test('Transmission: strikes an errored download, then removes and blocklists it at the limit', async ({
    api,
    mocks,
  }) => {
    test.setTimeout(300_000);

    await transmission.ready();
    await transmission.clearAllTorrents();

    const torrent = buildFolderTorrent(join(HOST_DOWNLOADS, 'transmission'), `errored-tr-${Date.now()}`);
    await transmission.addSeedingTorrent({
      metainfo: torrent.metainfo,
      savePath: CLIENT_DOWNLOADS,
      category: '',
      infoHash: torrent.infoHash,
    });

    let arranged: Arrangement | undefined;

    try {
      rmSync(torrent.contentPath, { recursive: true, force: true });
      await transmission.verifyTorrent(torrent.infoHash);

      // error 3 is a local failure, the only code the wrapper treats as errored.
      await expect
        .poll(async () => (await transmission.getError(torrent.infoHash)).error, {
          message: 'Transmission never reported a local error',
          timeout: 60_000,
        })
        .toBe(3);

      await stubHealthyLookingQueue(mocks, torrent.infoHash);
      arranged = await arrangeCleanuparr(
        api,
        'transmission',
        transmission.cleanuparrHost,
        'transmission',
        'transmission',
      );

      const startedAt = new Date().toISOString();

      // Each run below the limit strikes once and leaves the download in place.
      for (let strike = 1; strike < MAX_STRIKES; strike++) {
        expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

        await expect
          .poll(() => countEvents(api, 'ErroredStrike', torrent.infoHash), {
            message: `strike ${strike} was never recorded`,
            timeout: 60_000,
          })
          .toBe(strike);

        expect(await queueDeletes(mocks)).toHaveLength(0);
      }

      // Transmission accepted the start request; it stays stopped only because the data is gone.
      expect(appLogsSince(startedAt), 'the resume call was rejected by Transmission').not.toContain(
        RESUME_FAILURE_LOG,
      );

      expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

      await expect
        .poll(async () => (await queueDeletes(mocks)).length, {
          message: 'the download was never removed from the *arr queue at the strike limit',
          timeout: 60_000,
        })
        .toBeGreaterThan(0);

      const removal = (await queueDeletes(mocks))[0];
      expect(removal.url, 'removal should blocklist the release').toContain('blocklist=true');

      // Every fixture torrent is private, and delete-private defaults off.
      expect(removal.url, 'a private torrent should stay in the client by default').toContain(
        'removeFromClient=false',
      );
    } finally {
      await restore(api, arranged);
      await transmission.clearAllTorrents();
    }
  });

  test('Transmission: resets the strikes once the download recovers', async ({ api, mocks }) => {
    test.setTimeout(300_000);

    await transmission.ready();
    await transmission.clearAllTorrents();

    const name = `errored-reset-${Date.now()}`;
    const torrent = buildFolderTorrent(join(HOST_DOWNLOADS, 'transmission'), name);
    await transmission.addSeedingTorrent({
      metainfo: torrent.metainfo,
      savePath: CLIENT_DOWNLOADS,
      category: '',
      infoHash: torrent.infoHash,
    });

    let arranged: Arrangement | undefined;

    try {
      rmSync(torrent.contentPath, { recursive: true, force: true });
      await transmission.verifyTorrent(torrent.infoHash);
      await expect
        .poll(async () => (await transmission.getError(torrent.infoHash)).error, { timeout: 60_000 })
        .toBe(3);

      await stubHealthyLookingQueue(mocks, torrent.infoHash);
      arranged = await arrangeCleanuparr(
        api,
        'transmission',
        transmission.cleanuparrHost,
        'transmission',
        'transmission',
      );

      expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);
      await expect.poll(() => countEvents(api, 'ErroredStrike', torrent.infoHash), { timeout: 60_000 }).toBe(1);

      // Put the data back and clear the error, the way a reconnected drive would.
      buildFolderTorrent(join(HOST_DOWNLOADS, 'transmission'), name);
      await transmission.verifyTorrent(torrent.infoHash);
      await expect
        .poll(async () => (await transmission.getError(torrent.infoHash)).error, { timeout: 60_000 })
        .toBe(0);

      expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

      await expect
        .poll(() => countEvents(api, 'StrikeReset', torrent.infoHash), {
          message: 'the errored strikes were never reset after the download recovered',
          timeout: 60_000,
        })
        .toBeGreaterThan(0);
    } finally {
      await restore(api, arranged);
      await transmission.clearAllTorrents();
    }
  });

  test('qBittorrent: strikes a torrent reporting missing files and resumes it', async ({ api, mocks }) => {
    test.setTimeout(300_000);

    await qbit.ready();
    await qbit.clearAllTorrents();

    const torrent = buildFolderTorrent(join(HOST_DOWNLOADS, 'qbittorrent'), `errored-qbit-${Date.now()}`);

    // skip_checking makes qBittorrent trust the data is complete, so losing it is an error
    // rather than something still to download.
    await qbit.addSeedingTorrent({
      metainfo: torrent.metainfo,
      savePath: CLIENT_DOWNLOADS,
      category: '',
      infoHash: torrent.infoHash,
    });

    let arranged: Arrangement | undefined;

    try {
      await expect
        .poll(() => qbit.getState(torrent.infoHash), {
          message: 'qBittorrent never reported the torrent as complete',
          timeout: 60_000,
        })
        .toMatch(/UP$/);

      await expect
        .poll(() => hasPersistedResumeData(torrent.infoHash), {
          message: 'qBittorrent never wrote the resume data the restart has to reload',
          timeout: 60_000,
        })
        .toBe(true);

      rmSync(torrent.contentPath, { recursive: true, force: true });
      restartComposeService('qbittorrent');
      await qbit.ready();

      await expect
        .poll(() => qbit.getState(torrent.infoHash), {
          message: 'qBittorrent never reported missingFiles',
          timeout: 60_000,
        })
        .toBe('missingFiles');

      await stubHealthyLookingQueue(mocks, torrent.infoHash);
      arranged = await arrangeCleanuparr(api, 'qbittorrent', qbit.cleanuparrHost, 'admin', 'adminadmin');

      const startedAt = new Date().toISOString();
      expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

      await expect
        .poll(() => countEvents(api, 'ErroredStrike', torrent.infoHash), {
          message: 'the errored strike was never recorded',
          timeout: 60_000,
        })
        .toBe(1);

      // One strike is below the limit, so the download stays in the queue.
      expect(await queueDeletes(mocks)).toHaveLength(0);

      // qBittorrent accepted the resume call. How long libtorrent then takes to leave
      // missingFiles is its own business, so the spec does not wait on that.
      expect(appLogsSince(startedAt), 'the resume call was rejected by qBittorrent').not.toContain(
        RESUME_FAILURE_LOG,
      );
    } finally {
      await restore(api, arranged);
      await qbit.clearAllTorrents();
    }
  });
  test('Transmission: ignore private skips the check entirely', async ({ api, mocks }) => {
    test.setTimeout(300_000);

    const torrent = await arrangeErroredTransmissionTorrent(mocks, 'errored-ignore');
    let arranged: Arrangement | undefined;

    try {
      arranged = await arrangeCleanuparr(
        api,
        'transmission',
        transmission.cleanuparrHost,
        'transmission',
        'transmission',
        { ignorePrivate: true },
      );

      await strikeToTheLimit(api, mocks);
      // starts only after the last strike run has finished
      await runQueueCleaner(api, mocks);

      expect(await countEvents(api, 'ErroredStrike', torrent.infoHash)).toBe(0);
      expect(await queueDeletes(mocks)).toHaveLength(0);
    } finally {
      await restore(api, arranged);
      await transmission.clearAllTorrents();
    }
  });

  test('Transmission: delete private removes the torrent from the client too', async ({ api, mocks }) => {
    test.setTimeout(300_000);

    const torrent = await arrangeErroredTransmissionTorrent(mocks, 'errored-delete');
    let arranged: Arrangement | undefined;

    try {
      arranged = await arrangeCleanuparr(
        api,
        'transmission',
        transmission.cleanuparrHost,
        'transmission',
        'transmission',
        { deletePrivate: true },
      );

      await strikeToTheLimit(api, mocks);

      await expect
        .poll(async () => (await queueDeletes(mocks)).length, {
          message: 'the download was never removed from the *arr queue at the strike limit',
          timeout: 60_000,
        })
        .toBeGreaterThan(0);

      const removal = (await queueDeletes(mocks))[0];
      expect(removal.url).toContain('removeFromClient=true');
    } finally {
      await restore(api, arranged);
      await transmission.clearAllTorrents();
    }
  });

  test('Transmission: change category asks the *arr to recategorise instead of removing', async ({ api, mocks }) => {
    test.setTimeout(300_000);

    const torrent = await arrangeErroredTransmissionTorrent(mocks, 'errored-category');
    let arranged: Arrangement | undefined;

    try {
      arranged = await arrangeCleanuparr(
        api,
        'transmission',
        transmission.cleanuparrHost,
        'transmission',
        'transmission',
        { changeCategory: true },
      );

      await strikeToTheLimit(api, mocks);

      await expect
        .poll(async () => (await queueDeletes(mocks)).length, {
          message: 'the *arr was never asked to act at the strike limit',
          timeout: 60_000,
        })
        .toBeGreaterThan(0);

      const removal = (await queueDeletes(mocks))[0];
      expect(removal.url).toContain('changeCategory=true');
      expect(removal.url).toContain('removeFromClient=false');
    } finally {
      await restore(api, arranged);
      await transmission.clearAllTorrents();
    }
  });
  // The only e2e proof that errored strikes notify under their own toggle rather than the
  // stalled one, which is what they used to ride on.
  for (const { label, onErroredStrike, onStalledStrike, expected } of [
    { label: 'notifies when the errored strike toggle is on', onErroredStrike: true, onStalledStrike: false, expected: 1 },
    { label: 'stays silent when only the stalled strike toggle is on', onErroredStrike: false, onStalledStrike: true, expected: 0 },
  ]) {
    test(`Transmission: ${label}`, async ({ api, mocks }) => {
      test.setTimeout(300_000);

      const torrent = await arrangeErroredTransmissionTorrent(mocks, 'errored-notify');
      await mocks.notify.stub(NotificationStubs.gotifyMessageStub());

      let arranged: Arrangement | undefined;
      let providerId: string | undefined;

      try {
        arranged = await arrangeCleanuparr(
          api,
          'transmission',
          transmission.cleanuparrHost,
          'transmission',
          'transmission',
        );

        const created = await api.notifications.create('gotify', {
          name: `gotify-errored-${Date.now()}`,
          serverUrl: TEST_CONFIG.mocks.notifyUrl,
          applicationToken: 'gotify-app-token',
          onErroredStrike,
          onStalledStrike,
        });
        const createdBody = await created.text();
        expect(created.status, createdBody).toBeLessThan(300);
        providerId = JSON.parse(createdBody).id as string;

        expect((await api.jobs.trigger('QueueCleaner')).status).toBeLessThan(300);

        await expect
          .poll(() => countEvents(api, 'ErroredStrike', torrent.infoHash), {
            message: 'the errored strike was never recorded',
            timeout: 60_000,
          })
          .toBe(1);

        await expect
          .poll(async () => (await mocks.notify.findRequests({ method: 'POST', urlPath: '/message' })).length, {
            message: `expected ${expected} notification sends`,
            timeout: 30_000,
          })
          .toBe(expected);
      } finally {
        if (providerId) {
          await api.notifications.delete(providerId);
        }
        await restore(api, arranged);
        await transmission.clearAllTorrents();
      }
    });
  }
});
