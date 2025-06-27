import React from 'react';
import styles from './support.module.css';
import SupportHero from './SupportHero';
import DonationMethods from './DonationMethods';
import AlternativeSupport from './AlternativeSupport';

export default function SupportPage() {
  return (
    <div className={styles.supportPage}>
      <SupportHero />
      <div className={styles.contentContainer}>
        <DonationMethods />
        <AlternativeSupport />
      </div>
    </div>
  );
} 