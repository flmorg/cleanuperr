import React from 'react';
import styles from './documentation.module.css';

interface StepProps {
  title: string;
  children: React.ReactNode;
}

interface StepGuideProps {
  children: React.ReactElement<StepProps>[];
  className?: string;
}

export function Step({ title, children }: StepProps) {
  return (
    <div className={styles.step}>
      <div className={styles.stepNumber} />
      <div className={styles.stepContent}>
        <h4>{title}</h4>
        {children}
      </div>
    </div>
  );
}

export default function StepGuide({ children, className }: StepGuideProps) {
  return (
    <div className={`${styles.stepGuide} ${className || ''}`}>
      {children}
    </div>
  );
} 