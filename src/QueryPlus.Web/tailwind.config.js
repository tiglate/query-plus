/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.{cshtml,cs}",
    "./Views/**/*.{cshtml,cs}",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        navy: {
          DEFAULT: "#1C334D",
          50: "#E8EEF4",
          100: "#D1DDE9",
          200: "#A3BBD3",
          300: "#7599BD",
          400: "#4777A7",
          500: "#1C334D",
          600: "#172A40",
          700: "#122033",
          800: "#0C1626",
          900: "#070D19"
        },
        cyan: {
          DEFAULT: "#21BBEE",
          50: "#E9F8FD",
          100: "#D3F1FB",
          500: "#21BBEE",
          600: "#0EA5D8",
          700: "#0B84AD"
        },
        lime: {
          DEFAULT: "#DEE76C",
          500: "#DEE76C",
          600: "#C9D24A"
        },
        leaf: {
          DEFAULT: "#9FCD6B",
          500: "#9FCD6B",
          600: "#86B851"
        }
      },
      maxWidth: {
        desktop: "100%"
      },
      fontFamily: {
        sans: ["Inter", "Segoe UI", "system-ui", "sans-serif"]
      }
    }
  },
  plugins: []
};
