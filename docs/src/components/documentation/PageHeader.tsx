import React from 'react';
import styles from './documentation.module.css';

interface PageHeaderProps {
  title: string;
  subtitle: string;
  icon?: string;
  className?: string;
}

export default function PageHeader({ title, subtitle, icon, className }: PageHeaderProps) {
  return (
    <header className={`${styles.pageHeader} ${className || ''}`}>
      <div className={styles.headerContent}>
        <h1 className={styles.headerTitle}>
          {icon && (
            <span className={styles.headerIcon} role="img" aria-label={title}>
              {icon}
            </span>
          )}
          {title}
        </h1>
        <p className={styles.headerSubtitle}>{subtitle}</p>
      </div>
    </header>
  );
} 