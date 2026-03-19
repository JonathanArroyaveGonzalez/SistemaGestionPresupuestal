# Flujo de trabajo — Git Flow

Guía para el equipo de desarrollo backend **SistemaGestionPresupuestal** (.NET 10).

---

## Tabla de contenidos

1. [Ramas del proyecto](#1-ramas-del-proyecto)
2. [Reglas de oro](#2-reglas-de-oro)
3. [Convención de commits](#3-convención-de-commits)
4. [Flujo día a día](#4-flujo-día-a-día)
5. [Desarrollar una feature](#5-desarrollar-una-feature)
6. [Publicar un release](#6-publicar-un-release)
7. [Corregir un bug en producción (hotfix)](#7-corregir-un-bug-en-producción-hotfix)
8. [Pull Requests](#8-pull-requests)
9. [CI/CD — qué hace el pipeline](#9-cicd--qué-hace-el-pipeline)
10. [Resolución de conflictos](#10-resolución-de-conflictos)
11. [Referencia rápida de comandos](#11-referencia-rápida-de-comandos)

---

## 1. Ramas del proyecto

| Rama | Propósito | ¿Quién hace push directo? |
|------|-----------|--------------------------|
| `main` | Código en producción. Cada commit es una versión etiquetada. | Nadie. Solo merges desde `release/*` y `hotfix/*` |
| `develop` | Integración continua del equipo. Base para nuevas features. | Nadie. Solo merges desde `feature/*`, `release/*` y `hotfix/*` |
| `feature/*` | Una rama por funcionalidad o tarea. | El desarrollador asignado |
| `release/*` | Preparación de una versión. Solo bugfixes menores. | El responsable del release |
| `hotfix/*` | Corrección urgente en producción. | El desarrollador que atiende el incidente |

> **`main` y `develop` están protegidas.** No se puede hacer push directo. Todo cambio entra por Pull Request con al menos una aprobación y el pipeline de CI en verde.

---

## 2. Reglas de oro

- **Nunca** trabajes directamente sobre `main` o `develop`.
- **Siempre** parte desde la rama base correcta (ver sección 4).
- **Un PR por feature.** No acumules varias funcionalidades en una sola rama.
- **Commits pequeños y descriptivos.** Es más fácil revisar y revertir.
- **Sincroniza seguido.** Haz `git pull origin develop` en tu feature branch al menos una vez al día para reducir conflictos.
- **No borres ramas remotas manualmente.** Git Flow y el merge en GitHub se encargan.

---

## 3. Convención de commits

Usamos **Conventional Commits**. El formato es:

```
tipo(scope): descripción corta en imperativo
```

### Tipos permitidos

| Tipo | Cuándo usarlo |
|------|--------------|
| `feat` | Nueva funcionalidad visible para el usuario o el sistema |
| `fix` | Corrección de un bug |
| `refactor` | Cambio de código que no agrega funcionalidad ni corrige bug |
| `perf` | Mejora de rendimiento |
| `test` | Agregar o corregir tests |
| `docs` | Solo documentación |
| `build` | Cambios en dependencias o sistema de build |
| `ci` | Cambios en pipelines |
| `chore` | Tareas de mantenimiento (actualizar paquetes, etc.) |
| `revert` | Revertir un commit anterior |

### Ejemplos correctos

```
feat(presupuesto): agregar endpoint de aprobación de partidas
fix(auth): corregir null reference en validación de token JWT
refactor(reportes): extraer lógica de cálculo a servicio separado
test(usuarios): agregar tests para caso de email duplicado
docs(readme): actualizar instrucciones de configuración local
ci(github-actions): agregar step de cobertura de código
```

### Breaking changes

Si el cambio rompe compatibilidad con versiones anteriores, agrega `!` después del tipo:

```
feat!: cambiar estructura de respuesta del endpoint /presupuesto
```

> El template `.gitmessage` ya está configurado en el repo. Al hacer `git commit` (sin `-m`) se abre el editor con la guía incluida.

---

## 4. Flujo día a día

```
main ──────────────────────────────────────────────► producción
  └─► develop ──────────────────────────────────────► integración
          ├─► feature/mi-tarea ──────────────────────► tu trabajo diario
          ├─► feature/otra-tarea
          └─► release/x.y.z ──────────────────────────► pre-producción
```

**Regla de partida:**

| Si vas a... | Partes desde... | Terminas en... |
|-------------|-----------------|----------------|
| Desarrollar algo nuevo | `develop` | `develop` |
| Preparar un release | `develop` | `main` + `develop` |
| Corregir producción | `main` | `main` + `develop` |

---

## 5. Desarrollar una feature

### Paso 1 — Crear la rama

```bash
# Asegúrate de tener develop actualizado
git checkout develop
git pull origin develop

# Crea la rama de feature
git flow feature start nombre-de-la-feature

# Ejemplos de nombres:
# git flow feature start aprobacion-presupuesto
# git flow feature start autenticacion-jwt
# git flow feature start reporte-gastos-pdf
```

Esto crea la rama `feature/nombre-de-la-feature` a partir de `develop`.

### Paso 2 — Desarrollar y hacer commits

```bash
# Trabaja con normalidad
git add .
git commit
# Se abrirá el editor con el template de commit

# Mantén tu rama sincronizada con develop (hazlo frecuentemente)
git fetch origin
git rebase origin/develop
```

### Paso 3 — Publicar la rama y abrir PR

```bash
# Publica la rama en el remoto
git push origin feature/nombre-de-la-feature
```

Luego en GitHub:
1. Abre un **Pull Request** de `feature/nombre-de-la-feature` → `develop`
2. Completa el template del PR (descripción, tipo de cambio, checklist)
3. Asigna al menos un reviewer del equipo
4. Espera que el pipeline de CI esté en verde
5. El reviewer aprueba y hace el merge

### Paso 4 — Cerrar la feature (después del merge en GitHub)

```bash
git checkout develop
git pull origin develop
git branch -d feature/nombre-de-la-feature
```

> Si usas Git Flow CLI en lugar de GitHub para el merge: `git flow feature finish nombre-de-la-feature`

---

## 6. Publicar un release

Un release se crea cuando `develop` tiene suficientes features listas para salir a producción.

### Paso 1 — Crear la rama de release

```bash
git checkout develop
git pull origin develop

git flow release start 1.2.0
# La rama se llama release/1.2.0
```

### Paso 2 — Ajustes pre-release

En esta rama **solo se permiten**:
- Corrección de bugs menores encontrados en QA
- Actualización de versión en archivos (`.csproj`, `CHANGELOG.md`)
- Ajustes de configuración de despliegue

**No se agregan nuevas funcionalidades aquí.**

```bash
# Ejemplo: actualizar versión
# Edita el .csproj o Directory.Build.props
git commit -m "chore(release): bump version to 1.2.0"
```

### Paso 3 — Finalizar el release

```bash
git flow release finish 1.2.0
# Esto:
# 1. Hace merge a main y lo etiqueta como v1.2.0
# 2. Hace merge de vuelta a develop
# 3. Elimina la rama release/1.2.0

# Publica los cambios
git push origin main develop --tags
```

---

## 7. Corregir un bug en producción (hotfix)

Cuando hay un bug crítico en producción que no puede esperar al próximo release.

### Paso 1 — Crear el hotfix

```bash
git checkout main
git pull origin main

git flow hotfix start fix-calculo-iva
# La rama se llama hotfix/fix-calculo-iva
```

### Paso 2 — Corregir y hacer commit

```bash
# Corrige el bug
git add .
git commit -m "fix(presupuesto): corregir cálculo de IVA en partidas"
```

### Paso 3 — Finalizar el hotfix

```bash
git flow hotfix finish fix-calculo-iva
# Esto:
# 1. Hace merge a main y lo etiqueta (ej. v1.1.1)
# 2. Hace merge a develop para que el fix no se pierda
# 3. Elimina la rama hotfix/fix-calculo-iva

git push origin main develop --tags
```

---

## 8. Pull Requests

Todo cambio a `main` o `develop` pasa por un PR. Esto aplica a features, releases y hotfixes.

### Checklist antes de abrir un PR

- [ ] La rama tiene commits descriptivos siguiendo Conventional Commits
- [ ] El código compila sin warnings (`dotnet build`)
- [ ] Los tests pasan localmente (`dotnet test`)
- [ ] Agregué tests para los cambios que hice
- [ ] No dejé código comentado ni `TODO` sin registrar como issue
- [ ] Actualicé el `CHANGELOG.md` si es un feat o fix relevante

### Tamaño ideal de un PR

Un PR debería poder revisarse en menos de 30 minutos. Si tu PR toca más de 400 líneas, considera dividirlo en PRs más pequeños e incrementales.

### Como reviewer

- Aprueba solo si entiendes **qué** hace el código y **por qué**.
- Usa comentarios constructivos: sugiere alternativas, no solo señala errores.
- Si bloqueas el PR, explica qué hay que cambiar para desbloquearlo.
- El objetivo es mejorar el código, no demostrar quién sabe más.

---

## 9. CI/CD — qué hace el pipeline

El pipeline se ejecuta automáticamente en cada push y en cada PR hacia `main` o `develop`.

```
Push / PR
    │
    ▼
┌─────────────────────────────────┐
│ 1. Restore — dotnet restore     │
│ 2. Build   — dotnet build       │  ← Falla si hay warnings (TreatWarningsAsErrors)
│ 3. Test    — dotnet test        │  ← Falla si algún test no pasa
│ 4. Coverage — Codecov upload    │
└─────────────────────────────────┘
    │
    ▼
¿Todo verde? → PR puede hacerse merge
¿Algo rojo?  → No se puede hacer merge hasta corregirlo
```

**Si el pipeline falla en tu PR**, revisa la pestaña Actions en GitHub, busca el step que falló y corrige localmente antes de hacer push de nuevo.

---

## 10. Resolución de conflictos

Los conflictos ocurren cuando dos personas modificaron el mismo archivo. No son un error, son normales.

### Cómo minimizarlos

- Sincroniza tu rama con `develop` frecuentemente (`git rebase origin/develop`)
- Comunica al equipo si vas a refactorizar un archivo grande
- Divide las tareas de forma que cada persona toque módulos distintos

### Cómo resolverlos

```bash
# 1. Actualiza develop
git fetch origin
git rebase origin/develop

# 2. Si hay conflictos, Git los marcará en los archivos
# Abre VS, Rider o VS Code — tienen UI para resolver conflictos

# 3. Después de resolver cada archivo
git add archivo-resuelto.cs

# 4. Continúa el rebase
git rebase --continue

# 5. Publica la rama actualizada
git push origin feature/mi-feature --force-with-lease
```

> Usa siempre `--force-with-lease` en lugar de `--force`. Es más seguro porque falla si alguien más hizo push mientras tanto.

---

## 11. Referencia rápida de comandos

```bash
# === SETUP (una sola vez) ===
git flow init -d
git config commit.template .gitmessage

# === FEATURE ===
git flow feature start nombre           # crear
git push origin feature/nombre          # publicar
git flow feature finish nombre          # cerrar (solo si no usas PR en GitHub)

# === RELEASE ===
git flow release start 1.2.0
git flow release finish 1.2.0
git push origin main develop --tags

# === HOTFIX ===
git flow hotfix start nombre-del-fix
git flow hotfix finish nombre-del-fix
git push origin main develop --tags

# === DÍA A DÍA ===
git fetch origin                        # ver qué cambió
git rebase origin/develop               # sincronizar tu feature con develop
git push origin feature/nombre --force-with-lease  # publicar después de rebase

# === UTILIDADES ===
git log --oneline --graph --all         # ver el árbol de ramas
git stash                               # guardar cambios temporalmente
git stash pop                           # recuperar cambios guardados
```

---

## ¿Dudas?

Abre un issue en el repositorio con la etiqueta `question` o consulta con el líder técnico del equipo.