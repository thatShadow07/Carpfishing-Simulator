# CLAUDE.md — Regras do Projeto (Carp Fishing Game)

Este ficheiro define o contexto, regras e limites do projeto para Claude.

## Projeto

Jogo de carpfishing em primeira pessoa, realista mas acessível, focado numa experiência imersiva completa: preparar → viajar → escolher spot → montar equipamento → montar rig → pescar → combater → capturar → registar → progredir.

**Deadline da vertical slice: 1 de março de 2027.**

## Stack

- Unity 6
- C#
- Blender
- Primeira pessoa
- Alvo: PCs de gama média

## Papéis

| Quem | Papel |
|---|---|
| Bruno | Decide o rumo, trabalha no Unity, testa e aprova mudanças |
| ChatGPT | Arquitetura, sistemas, C#, Unity, debugging e planeamento |
| Claude | Code review, refactoring, documentação e análise multi-ficheiro |
| GitHub | Fonte de verdade do projeto |

Bruno está a aprender programação. Explicações devem ser progressivas e claras.

## Regras de ouro

1. Nenhuma IA muda a arquitetura sem explicar primeiro a alteração e o motivo.
2. Não implementar itens fora da fase atual do roadmap; adicionar ao backlog em vez disso.
3. Código simples, modular, legível e expansível.
4. Uma fase de cada vez.
5. Problemas fora do âmbito devem ser assinalados e sugeridos, não corrigidos silenciosamente.
6. Não introduzir sistemas novos que não foram pedidos.
7. Não reescrever código não relacionado com o pedido atual.

## Estrutura

```text
Carpfishing-Simulator/
├── Assets/
│   ├── Art/
│   ├── Audio/
│   ├── Materials/
│   ├── Models/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Player/
│   │   ├── Fishing/
│   │   ├── Fish/
│   │   ├── Equipment/
│   │   ├── Rigging/
│   │   ├── World/
│   │   ├── Progression/
│   │   └── UI/
│   ├── Settings/
│   └── UI/
├── Docs/
├── ProjectSettings/
├── Packages/
├── UserSettings/
├── GAME_DESIGN.md
├── ROADMAP.md
├── ARCHITECTURE.md
├── CLAUDE.md
├── AGENTS.md
└── README.md
```

## C# / Unity

- PascalCase para classes, métodos e propriedades públicas.
- camelCase para campos privados e parâmetros.
- Um MonoBehaviour por ficheiro; nome do ficheiro = nome da classe.
- Preferir composição e componentes pequenos.
- Comentar o porquê, não o óbvio.
- Evitar dependências rígidas entre sistemas.

## Workflow

1. Inspecionar os ficheiros relevantes antes de editar.
2. Confirmar em que fase do roadmap a alteração se enquadra.
3. Fazer mudanças pequenas e focadas.
4. Testar antes de integrar sistemas maiores.
5. Atualizar documentação quando comportamento ou arquitetura mudar.
6. Usar commits pequenos e descritivos.

## Colaboração

Claude deve tratar o código e documentação existentes como fonte de verdade e considerar que ChatGPT também trabalha neste repositório. Se uma alteração estrutural for necessária, explicá-la antes de a implementar.

## Frase do projeto

> Não queremos apenas um jogo onde pescas carpas. Queremos um jogo onde vais pescar.
