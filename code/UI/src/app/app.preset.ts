import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

const Noir = definePreset(Aura, {
    semantic: {
        // Use purple as the primary color palette
        primary: {
            50: '{violet.50}',
            100: '{violet.100}',
            200: '{violet.200}',
            300: '{violet.300}',
            400: '{violet.400}',
            500: '{violet.500}',
            600: '{violet.600}',
            700: '{violet.700}',
            800: '{violet.800}',
            900: '{violet.900}',
            950: '{violet.950}'
        },
        colorScheme: {
            // Skip light mode configuration since we're only using dark mode
            dark: {
                // Base colors for dark mode
                surface: {
                    ground: '#121212',       // Very dark gray for main background
                    section: '#1a1a1a',      // Slightly lighter for sections
                    card: '#212121',         // Card background
                    overlay: '#262626',      // Overlay surface
                    border: '#383838',       // Border color
                    hover: 'rgba(255,255,255,.03)' // Subtle hover effect
                },
                // Purple accent configuration
                primary: {
                    color: '{violet.500}',       // Main purple (medium brightness)
                    inverseColor: '#ffffff',     // White text on purple backgrounds
                    hoverColor: '{violet.400}',  // Lighter on hover
                    activeColor: '{violet.300}'  // Even lighter when active
                },
                highlight: {
                    background: 'rgba(124, 58, 237, 0.16)',  // Subtle purple highlight
                    focusBackground: 'rgba(124, 58, 237, 0.24)', // Slightly stronger when focused
                    color: 'rgba(255,255,255,.87)',
                    focusColor: 'rgba(255,255,255,.87)'
                }
            }
        }
    }
});

export default Noir;