# Elysium CS2 API

Документация публичных контрактов проекта собирается из XML-комментариев исходного кода.

- [Каталог событий](events.md) описывает точку вызова, частоту, нагрузку, риск и поток выполнения каждого события.
- [Сборка и развёртывание](build-and-deployment.md) описывает создание компактного runtime, установку на сервер и безопасный откат.
- [API reference](api/index.md) содержит сгенерированные DocFX страницы интерфейсов, контекстов и других публичных типов.

Для локальной сборки используйте `./docs/build.sh` на Linux/macOS или `./docs/build.ps1` в PowerShell. Готовый сайт появится в `docs/_site`.

## Custom HUD

Общий API баннеров и рекламы, примеры вызовов и установка ресурсов: [CustomHud.Core](https://github.com/elysium-corp/cs2-zombie-plague/blob/develop/CustomHud.Core/README.md)
