#!/bin/bash
# ─────────────────────────────────────────────────────────────────
# Script de release TibiaTimer
# Uso: ./release.sh 1.0.0
# ─────────────────────────────────────────────────────────────────
set -e

VERSION=${1:-"1.0.0"}
PUBLISH_DIR="./publish/win-x64"
RELEASES_DIR="./releases"

echo "==> Publicando versão $VERSION para win-x64..."
dotnet publish src/TimerApp.Desktop/TimerApp.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=none -p:DebugSymbols=false \
  -p:Version=$VERSION \
  -o $PUBLISH_DIR

# Remove .pdb desnecessários
rm -f $PUBLISH_DIR/*.pdb

echo "==> Empacotando com Velopack..."
export PATH="$PATH:$HOME/.dotnet/tools"
vpk pack \
  --packId "TibiaTimer" \
  --packVersion "$VERSION" \
  --packDir "$PUBLISH_DIR" \
  --mainExe "TimerApp.Desktop.exe" \
  --outputDir "$RELEASES_DIR" \
  --icon "src/TimerApp.Desktop/Assets/icon.ico" 2>/dev/null || \
vpk pack \
  --packId "TibiaTimer" \
  --packVersion "$VERSION" \
  --packDir "$PUBLISH_DIR" \
  --mainExe "TimerApp.Desktop.exe" \
  --outputDir "$RELEASES_DIR"

echo ""
echo "==> Pronto! Arquivos em: $RELEASES_DIR"
echo ""
echo "Para publicar no GitHub:"
echo "  1. Crie o repositório em https://github.com/new"
echo "  2. Atualize AppConfig.GitHubRepoUrl com a URL do repo"
echo "  3. Execute:"
echo "     vpk upload github \\"
echo "       --repoUrl https://github.com/SEU-USUARIO/TibiaTimer \\"
echo "       --publish \\"
echo "       --releaseName \"TibiaTimer v$VERSION\" \\"
echo "       --tag \"v$VERSION\""
echo ""
echo "     (precisa do token GitHub: --token ghp_SEU_TOKEN)"
