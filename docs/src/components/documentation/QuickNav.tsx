import React from 'react';
import styles from './documentation.module.css';

interface NavItem {
  label: string;
  href: string;
  icon?: string;
}

interface QuickNavProps {
  title?: string;
  items: NavItem[];
  className?: string;
}

export default function QuickNav({ 
  title = "Quick Navigation", 
  items, 
  className 
}: QuickNavProps) {
  return (
    <nav className={`${styles.quickNav} ${className || ''}`}>
      <h4 className={styles.quickNavTitle}>
        <span role="img" aria-label="Navigation">🧭</span>
        {title}
      </h4>
      <ul className={styles.quickNavList}>
        {items.map((item, index) => (
          <li key={index} className={styles.quickNavItem}>
            <a href={item.href}>
              {item.icon && (
                <span role="img" aria-label={item.label}>
                  {item.icon}
                </span>
              )}
              {item.label}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
} 