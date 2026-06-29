#!/bin/bash
# ─────────────────────────────────────────────────────────────────
# Script de release TibiaTimer
# Uso: ./release.sh 1.0.0
#
# O build e empacotamento rodam automaticamente no GitHub Actions
# (Windows) após o push da tag.
# ─────────────────────────────────────────────────────────────────
set -e

VERSION=${1:?"Informe a versão. Exemplo: ./release.sh 1.0.0"}
TAG="v$VERSION"

# Verifica se já compilou localmente (sanidade)
echo "==> Verificando compilação local..."
dotnet build src/TimerApp.Desktop/TimerApp.Desktop.csproj -c Release --nologo -v q

echo "==> Criando tag $TAG..."
git tag "$TAG"
git push origin "$TAG"

echo ""
echo "==> Tag $TAG enviada!"
echo "    Acompanhe o build em:"
echo "    https://github.com/galvaodev/timertibia/actions"
echo ""
echo "    Quando finalizar (~3 min), o release estará em:"
echo "    https://github.com/galvaodev/timertibia/releases"
