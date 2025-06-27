import React from 'react';
import styles from './documentation.module.css';

interface InstallationMethodProps {
  title: string;
  description: string;
  icon: string;
  features?: string[];
  recommended?: boolean;
  color?: string;
  children: React.ReactNode;
  className?: string;
}

export default function InstallationMethod({
  title,
  description,
  icon,
  features = [],
  recommended = false,
  color,
  children,
  className
}: InstallationMethodProps) {
  const cardClass = `${styles.methodCard} ${recommended ? styles.recommended : ''} ${className || ''}`;
  
  const cardStyle = color ? { '--method-color': color } as React.CSSProperties : {};

  return (
    <div className={cardClass} style={cardStyle}>
      <div className={styles.methodHeader}>
        <span className={styles.methodIcon} role="img" aria-label={title}>
          {icon}
        </span>
        <h3 className={styles.methodTitle}>{title}</h3>
      </div>
      
      <p className={styles.methodDescription}>{description}</p>
      
      {features.length > 0 && (
        <ul className={styles.methodFeatures}>
          {features.map((feature, index) => (
            <li key={index}>{feature}</li>
          ))}
        </ul>
      )}
      
      <div>{children}</div>
    </div>
  );
} 