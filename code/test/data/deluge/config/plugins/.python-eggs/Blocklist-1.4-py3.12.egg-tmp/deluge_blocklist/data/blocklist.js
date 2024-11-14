/**
 * blocklist.js
 *
 * Copyright (C) Omar Alvarez 2014 <omar.alvarez@udc.es>
 *
 * This file is part of Deluge and is licensed under GNU General Public License 3.0, or later, with
 * the additional special exception to link portions of this program with the OpenSSL library.
 * See LICENSE for more details.
 *
 */

Ext.ns('Deluge.ux.preferences');

/**
 * @class Deluge.ux.preferences.BlocklistPage
 * @extends Ext.Panel
 */
Deluge.ux.preferences.BlocklistPage = Ext.extend(Ext.Panel, {
    title: _('Blocklist'),
    header: false,
    layout: 'fit',
    border: false,
    autoScroll: true,

    initComponent: function () {
        Deluge.ux.preferences.BlocklistPage.superclass.initComponent.call(this);

        this.URLFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('General'),
            autoHeight: true,
            defaultType: 'textfield',
            style: 'margin-top: 3px; margin-bottom: 0px; padding-bottom: 0px;',
            autoWidth: true,
            labelWidth: 40,
        });

        this.URL = this.URLFset.add({
            fieldLabel: _('URL:'),
            labelSeparator: '',
            name: 'url',
            width: '80%',
        });

        this.SettingsFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('Settings'),
            autoHeight: true,
            defaultType: 'spinnerfield',
            style: 'margin-top: 3px; margin-bottom: 0px; padding-bottom: 0px;',
            autoWidth: true,
            labelWidth: 160,
        });

        this.checkListDays = this.SettingsFset.add({
            fieldLabel: _('Check for new list every (days):'),
            labelSeparator: '',
            name: 'check_list_days',
            value: 4,
            decimalPrecision: 0,
            width: 80,
        });

        this.chkImportOnStart = this.SettingsFset.add({
            xtype: 'checkbox',
            fieldLabel: _('Import blocklist on startup'),
            name: 'check_import_startup',
        });

        this.OptionsFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('Options'),
            autoHeight: true,
            defaultType: 'button',
            style: 'margin-top: 3px; margin-bottom: 0px; padding-bottom: 0px;',
            autoWidth: false,
            width: '80%',
            labelWidth: 0,
        });

        this.checkDownload = this.OptionsFset.add({
            fieldLabel: _(''),
            name: 'check_download',
            xtype: 'container',
            layout: 'hbox',
            margins: '4 0 0 5',
            items: [
                {
                    xtype: 'button',
                    text: ' Check Download and Import ',
                    scale: 'medium',
                },
                {
                    xtype: 'box',
                    autoEl: {
                        tag: 'img',
                        src: '../icons/ok.png',
                    },
                    margins: '4 0 0 3',
                },
            ],
        });

        this.forceDownload = this.OptionsFset.add({
            fieldLabel: _(''),
            name: 'force_download',
            text: ' Force Download and Import ',
            margins: '2 0 0 0',
            //icon: '../icons/blocklist_import24.png',
            scale: 'medium',
        });

        this.ProgressFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('Info'),
            autoHeight: true,
            defaultType: 'progress',
            style: 'margin-top: 1px; margin-bottom: 0px; padding-bottom: 0px;',
            autoWidth: true,
            labelWidth: 0,
            hidden: true,
        });

        this.downProgBar = this.ProgressFset.add({
            fieldLabel: _(''),
            name: 'progress_bar',
            width: '90%',
        });

        this.InfoFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('Info'),
            autoHeight: true,
            defaultType: 'label',
            style: 'margin-top: 0px; margin-bottom: 0px; padding-bottom: 0px;',
            labelWidth: 60,
        });

        this.lblFileSize = this.InfoFset.add({
            fieldLabel: _('File Size:'),
            labelSeparator: '',
            name: 'file_size',
        });

        this.lblDate = this.InfoFset.add({
            fieldLabel: _('Date:'),
            labelSeparator: '',
            name: 'date',
        });

        this.lblType = this.InfoFset.add({
            fieldLabel: _('Type:'),
            labelSeparator: '',
            name: 'type',
        });

        this.lblURL = this.InfoFset.add({
            fieldLabel: _('URL:'),
            labelSeparator: '',
            name: 'lbl_URL',
        });

        this.WhitelistFset = this.add({
            xtype: 'fieldset',
            border: false,
            title: _('Whitelist'),
            autoHeight: true,
            defaultType: 'editorgrid',
            style: 'margin-top: 3px; margin-bottom: 0px; padding-bottom: 0px;',
            autoWidth: true,
            labelWidth: 0,
            items: [
                {
                    fieldLabel: _(''),
                    name: 'whitelist',
                    margins: '2 0 5 5',
                    height: 100,
                    width: 260,
                    autoExpandColumn: 'ip',
                    viewConfig: {
                        emptyText: _('Add an IP...'),
                        deferEmptyText: false,
                    },
                    colModel: new Ext.grid.ColumnModel({
                        columns: [
                            {
                                id: 'ip',
                                header: _('IP'),
                                dataIndex: 'ip',
                                sortable: true,
                                hideable: false,
                                editable: true,
                                editor: {
                                    xtype: 'textfield',
                                },
                            },
                        ],
                    }),
                    selModel: new Ext.grid.RowSelectionModel({
                        singleSelect: false,
                        moveEditorOnEnter: false,
                    }),
                    store: new Ext.data.ArrayStore({
                        autoDestroy: true,
                        fields: [{ name: 'ip' }],
                    }),
                    listeners: {
                        afteredit: function (e) {
                            e.record.commit();
                        },
                    },
                    setEmptyText: function (text) {
                        if (this.viewReady) {
                            this.getView().emptyText = text;
                            this.getView().refresh();
                        } else {
                            Ext.apply(this.viewConfig, { emptyText: text });
                        }
                    },
                    loadData: function (data) {
                        this.getStore().loadData(data);
                        if (this.viewReady) {
                            this.getView().updateHeaders();
                        }
                    },
                },
            ],
        });

        this.ipButtonsContainer = this.WhitelistFset.add({
            xtype: 'container',
            layout: 'hbox',
            margins: '4 0 0 5',
            items: [
                {
                    xtype: 'button',
                    text: ' Add IP ',
                    margins: '0 5 0 0',
                },
                {
                    xtype: 'button',
                    text: ' Delete IP ',
                },
            ],
        });

        this.updateTask = Ext.TaskMgr.start({
            interval: 2000,
            run: this.onUpdate,
            scope: this,
        });

        this.on('show', this.updateConfig, this);

        this.ipButtonsContainer.getComponent(0).setHandler(this.addIP, this);
        this.ipButtonsContainer.getComponent(1).setHandler(this.deleteIP, this);

        this.checkDownload.getComponent(0).setHandler(this.checkDown, this);
        this.forceDownload.setHandler(this.forceDown, this);
    },

    onApply: function () {
        var config = {};

        config['url'] = this.URL.getValue();
        config['check_after_days'] = this.checkListDays.getValue();
        config['load_on_start'] = this.chkImportOnStart.getValue();

        var ipList = [];
        var store = this.WhitelistFset.getComponent(0).getStore();

        for (var i = 0; i < store.getCount(); i++) {
            var record = store.getAt(i);
            var ip = record.get('ip');
            ipList.push(ip);
        }

        config['whitelisted'] = ipList;

        deluge.client.blocklist.set_config(config);
    },

    onOk: function () {
        this.onApply();
    },

    onUpdate: function () {
        deluge.client.blocklist.get_status({
            success: function (status) {
                if (status['state'] == 'Downloading') {
                    this.InfoFset.hide();
                    this.checkDownload.getComponent(0).setDisabled(true);
                    this.checkDownload.getComponent(1).hide();
                    this.forceDownload.setDisabled(true);

                    this.ProgressFset.show();
                    this.downProgBar.updateProgress(
                        status['file_progress'],
                        'Downloading '
                            .concat((status['file_progress'] * 100).toFixed(2))
                            .concat('%'),
                        true
                    );
                } else if (status['state'] == 'Importing') {
                    this.InfoFset.hide();
                    this.checkDownload.getComponent(0).setDisabled(true);
                    this.checkDownload.getComponent(1).hide();
                    this.forceDownload.setDisabled(true);

                    this.ProgressFset.show();
                    this.downProgBar.updateText(
                        'Importing '.concat(status['num_blocked'])
                    );
                } else if (status['state'] == 'Idle') {
                    this.ProgressFset.hide();
                    this.checkDownload.getComponent(0).setDisabled(false);
                    this.forceDownload.setDisabled(false);
                    if (status['up_to_date']) {
                        this.checkDownload.getComponent(1).show();
                        this.checkDownload.doLayout();
                    } else {
                        this.checkDownload.getComponent(1).hide();
                    }
                    this.InfoFset.show();
                    this.lblFileSize.setText(fsize(status['file_size']));
                    this.lblDate.setText(fdate(status['file_date']));
                    this.lblType.setText(status['file_type']);
                    this.lblURL.setText(
                        status['file_url'].substr(0, 40).concat('...')
                    );
                }
            },
            scope: this,
        });
    },

    checkDown: function () {
        this.onApply();
        deluge.client.blocklist.check_import();
    },

    forceDown: function () {
        this.onApply();
        deluge.client.blocklist.check_import((force = true));
    },

    updateConfig: function () {
        deluge.client.blocklist.get_config({
            success: function (config) {
                this.URL.setValue(config['url']);
                this.checkListDays.setValue(config['check_after_days']);
                this.chkImportOnStart.setValue(config['load_on_start']);

                var data = [];
                var keys = Ext.keys(config['whitelisted']);
                for (var i = 0; i < keys.length; i++) {
                    var key = keys[i];
                    data.push([config['whitelisted'][key]]);
                }

                this.WhitelistFset.getComponent(0).loadData(data);
            },
            scope: this,
        });

        deluge.client.blocklist.get_status({
            success: function (status) {
                this.lblFileSize.setText(fsize(status['file_size']));
                this.lblDate.setText(fdate(status['file_date']));
                this.lblType.setText(status['file_type']);
                this.lblURL.setText(
                    status['file_url'].substr(0, 40).concat('...')
                );
            },
            scope: this,
        });
    },

    addIP: function () {
        var store = this.WhitelistFset.getComponent(0).getStore();
        var IP = store.recordType;
        var i = new IP({
            ip: '',
        });
        this.WhitelistFset.getComponent(0).stopEditing();
        store.insert(0, i);
        this.WhitelistFset.getComponent(0).startEditing(0, 0);
    },

    deleteIP: function () {
        var selections = this.WhitelistFset.getComponent(0)
            .getSelectionModel()
            .getSelections();
        var store = this.WhitelistFset.getComponent(0).getStore();

        this.WhitelistFset.getComponent(0).stopEditing();
        for (var i = 0; i < selections.length; i++) store.remove(selections[i]);
        store.commitChanges();
    },

    onDestroy: function () {
        Ext.TaskMgr.stop(this.updateTask);

        deluge.preferences.un('show', this.updateConfig, this);

        Deluge.ux.preferences.BlocklistPage.superclass.onDestroy.call(this);
    },
});

Deluge.plugins.BlocklistPlugin = Ext.extend(Deluge.Plugin, {
    name: 'Blocklist',

    onDisable: function () {
        deluge.preferences.removePage(this.prefsPage);
    },

    onEnable: function () {
        this.prefsPage = deluge.preferences.addPage(
            new Deluge.ux.preferences.BlocklistPage()
        );
    },
});

Deluge.registerPlugin('Blocklist', Deluge.plugins.BlocklistPlugin);
