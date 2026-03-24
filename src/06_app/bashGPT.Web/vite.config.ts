import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    outDir: '../bashGPT.Server/wwwroot',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: 'bundle.js',
        chunkFileNames: 'bundle.js',
        assetFileNames: 'bundle.[ext]',
      },
    },
    target: 'es2020',
  },
})
