/** @type {import('tailwindcss').Config} */
export default {
    content: [
        "./pages/**/*.{js,ts,jsx,tsx,mdx}",
        "./components/**/*.{js,ts,jsx,tsx,mdx}",
        "./app/**/*.{js,ts,jsx,tsx,mdx}",
    ],
    theme: {
        extend: {
            colors: {
                background: "#0F0F0F",
                foreground: "var(--foreground)",
                defaultBlack: "#0F0F0F",
                offBlack: "#1D1D1D",
            },
        },
    },
    plugins: [],
};
