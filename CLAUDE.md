# CLAUDE.md — Regras do Projeto (Carp Fishing Game)

> Este ficheiro existe para que, sempre que o Claude for chamado a trabalhar neste
> repositório, saiba imediatamente o contexto, as regras e os limites do projeto —
> sem precisar de reler tudo do zero nem inventar arquitetura própria.

---

## 1. O que é este projeto

Jogo de **carpfishing em primeira pessoa**, realista mas acessível, focado numa
experiência imersiva completa: preparar → viajar → escolher spot → montar
equipamento → montar rig → pescar → combater → capturar → registar → progredir.

Documento de referência completo: **`GAME_DESIGN.md`** (GDD v0.1). Este `CLAUDE.md`
não substitui o GDD — complementa-o com regras de trabalho.

**Deadline da vertical slice: 1 de março de 2027.**
Fase atual: ver `ROADMAP.md`.

---

## 2. Stack técnica

- **Motor:** Unity 6
- **Linguagem:** C#
- **Modelação:** Blender
- **Perspetiva:** primeira pessoa
- **Alvo de performance:** PCs de gama média (não é fotorealismo extremo nem
  hardware topo de gama)

---

## 3. Quem faz o quê

| Quem | Papel |
|---|---|
| **Bruno** | Decide o rumo, mexe no Unity, testa o jogo, aprova mudanças |
| **ChatGPT** | Arquitetura, sistemas, C#, Unity, debugging, planeamento do dia a dia |
| **Claude** | Revisão de código, refactoring, análise de vários scripts em conjunto, documentação, deteção de problemas arquiteturais, implementação de sistemas maiores quando pedido |
| **GitHub** | Histórico central do projeto — a fonte de verdade do código |

Bruno está a aprender programação. Explicações devem ser progressivas e claras,
não apenas "aqui está o código, copia".

---

## 4. Regras de ouro (não negociáveis)

1. **Nenhuma IA muda a arquitetura sem primeiro explicar a alteração e porquê.**
2. **Não implementar itens do backlog fora da fase atual do roadmap**, mesmo que
   pareçam boas ideias — vão para `ROADMAP.md` → secção Backlog.
3. **Código simples, modular, legível e expansível**, adequado a alguém a
   aprender — nunca "mais profissional só por ser mais complexo".
4. **Não fazer tudo ao mesmo tempo.** Uma fase de cada vez (ver secção 28 do GDD).
5. Se o Claude notar um problema de arquitetura fora do âmbito do pedido atual,
   deve **assinalar o problema e sugerir**, não corrigir silenciosamente sem avisar.

---

## 5. Estrutura do repositório (proposta inicial)

```text
CarpFishing/
├── Assets/
│   ├── Scripts/
│   │   ├── Player/
│   │   ├── Fishing/
│   │   ├── FishAI/
│   │   ├── Inventory/
│   │   ├── Shop/
│   │   └── UI/
│   ├── Models/
│   ├── Materials/
│   ├── Prefabs/
│   └── Scenes/
├── ProjectSettings/
├── GAME_DESIGN.md
├── ROADMAP.md
├── ARCHITECTURE.md
├── CLAUDE.md
└── README.md
```

Esta estrutura pode evoluir — se mudar, atualizar este ficheiro e explicar porquê
em `ARCHITECTURE.md`.

---

## 6. Convenções de código (C# / Unity)

- **PascalCase** para classes, métodos e propriedades públicas.
- **camelCase** para variáveis privadas e parâmetros.
- Um `MonoBehaviour` por ficheiro, nome do ficheiro = nome da classe.
- Preferir **composição** (componentes pequenos) a classes gigantes que fazem tudo.
- Comentar o **porquê**, não o óbvio ("porque é que isto existe", não "isto soma 1").
- Evitar dependências rígidas entre sistemas (ex: o sistema de combate não deve
  precisar de saber diretamente da loja).

---

## 7. Quando o Claude é chamado para rever código

Ao pedir revisão/refactoring, o Claude deve:

1. Perceber em que fase do roadmap isto se encaixa.
2. Verificar se a alteração respeita a estrutura de pastas e convenções acima.
3. Apontar problemas de arquitetura, performance ou legibilidade — com explicação
   simples, pensando que Bruno está a aprender.
4. Propor a alteração antes de a reescrever por completo, quando for uma mudança
   estrutural grande.
5. Nunca introduzir sistemas novos "de bónus" que não foram pedidos.

---

## 8. Backlog (não implementar ainda)

Ver lista viva em `ROADMAP.md`. Exemplos atuais: barcos, sonar, multiplayer, mais
lagos, competições, mapas portugueses, carpas únicas, sistema de troféus.

---

## 9. Frase que define o projeto

> "Não queremos apenas um jogo onde pescas carpas. Queremos um jogo onde vais pescar."
