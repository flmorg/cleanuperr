import React from 'react';
import styles from './support.module.css';
import DonationCard from './DonationCard';
import CryptoModal from './CryptoModal';
import { donationMethods } from './config/donationConfig';
import { useModal } from './hooks/useModal';

export default function DonationMethods() {
  const { isOpen, openModal, closeModal } = useModal();

  return (
    <section className={styles.donationMethodsSection}>
      <h2 className={styles.sectionTitle}>Support Our Development</h2>
      <div className={styles.donationGrid}>
        {donationMethods.map((method) => (
          <DonationCard
            key={method.id}
            method={method}
            onOpenModal={method.type === 'modal' ? openModal : undefined}
          />
        ))}
      </div>
      
      <CryptoModal isOpen={isOpen} onClose={closeModal} />
    </section>
  );
} 