// Generates ../console.offline.css so the console looks the same without an internet connection.
// Run: cd tools && npm install && npm run build
module.exports = {
  content: ['../console.html', '../console-live.js', '../review-deck.js', '../../WebConsole.cs'],
  plugins: [require('@tailwindcss/forms'), require('@tailwindcss/container-queries')],
  theme: {
        extend: {
          colors: {
            void: {
              980: '#020508',
              950: '#05080e',
              900: '#0a0f18',
              850: '#0f1724',
              800: '#142033',
              750: '#1a283f',
              700: '#223552',
              600: '#2e4568',
              500: '#43618f',
            },
            chassis: {
              rim: '#242a35',
              dark: '#11151c',
              plate: '#181e28',
              groove: '#0c0f14',
              screw: '#3d4655'
            },
            cyanGlow: '#38bdf8',
            spectralCyan: '#67e8f9',
            phosphor: '#10b981',
            phosphorBright: '#00ff88',
            phosphorAmber: '#f59e0b',
            spectralAmber: '#f59e0b',
            spectralRed: '#ef4444',
            bloodTally: '#ff2a2a',
          },
          fontFamily: {
            mono: ['"Share Tech Mono"', '"JetBrains Mono"', 'monospace'],
            code: ['"JetBrains Mono"', 'monospace'],
            sans: ['"Space Grotesk"', 'sans-serif'],
          },
          boxShadow: {
            'glow-cyan': '0 0 16px -2px rgba(56, 189, 248, 0.55)',
            'glow-green': '0 0 16px -2px rgba(0, 255, 136, 0.55)',
            'glow-amber': '0 0 16px -2px rgba(245, 158, 11, 0.55)',
            'glow-red': '0 0 18px -2px rgba(239, 68, 68, 0.65)',
            'tactile-chassis': 'inset 0 1px 1px rgba(255,255,255,0.12), inset 0 -3px 6px rgba(0,0,0,0.9), 0 20px 40px rgba(0,0,0,0.85)',
            'tactile-in': 'inset 0 2px 6px rgba(0,0,0,0.9), inset 0 -1px 2px rgba(255,255,255,0.06)',
            'tactile-out': '0 4px 10px rgba(0,0,0,0.7), inset 0 1px 1px rgba(255,255,255,0.18)',
            'chassis-bezel': 'inset 2px 2px 4px rgba(255,255,255,0.08), inset -2px -2px 6px rgba(0,0,0,0.95), 0 8px 16px rgba(0,0,0,0.75)',
            'knob-3d': '0 8px 14px rgba(0,0,0,0.85), inset 0 2px 2px rgba(255,255,255,0.25), inset 0 -3px 4px rgba(0,0,0,0.9)',
          }
        }
      }
};
