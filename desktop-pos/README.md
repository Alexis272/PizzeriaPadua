# Pizzería Padua para Windows

La aplicación de escritorio es una envoltura Electron de la versión web publicada
en Railway. La web continúa funcionando de forma independiente en el navegador.

## Desarrollo

```powershell
npm install
npm start
```

Para probar una instancia local:

```powershell
$env:PIZZERIA_PADUA_URL="http://localhost:5000/PizzeriaPadua/"
npm start
```

## Instalador

```powershell
npm run dist
```

El instalador se genera en `desktop-pos/dist/Pizzeria-Padua-Setup-1.0.0.exe`.
