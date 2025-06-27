import React from 'react';
import styles from './documentation.module.css';

interface ConfigSectionProps {
  id?: string;
  title: string;
  description?: string;
  icon?: string;
  badge?: 'required' | 'optional' | 'advanced';
  children: React.ReactNode;
  className?: string;
}

export default function ConfigSection({ 
  id, 
  title, 
  description, 
  icon, 
  badge, 
  children, 
  className 
}: ConfigSectionProps) {
  return (
    <section 
      id={id} 
      className={`${styles.configSection} ${className || ''}`}
    >
      <div className={styles.configHeader}>
        <h3 className={styles.configTitle}>
          {icon && (
            <span className={styles.configIcon} role="img" aria-label={title}>
              {icon}
            </span>
          )}
          {title}
        </h3>
        {badge && (
          <span className={`${styles.configBadge} ${styles[badge]}`}>
            {badge}
          </span>
        )}
      </div>
      {description && (
        <p className={styles.configDescription}>{description}</p>
      )}
      <div>{children}</div>
    </section>
  );
} 