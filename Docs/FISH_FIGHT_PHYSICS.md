# Fish Fight Physics

## Objetivo

O combate deve transmitir peso, direção, força e fadiga sem depender de uma coleção de multiplicadores para corrigir comportamentos isolados.

O modelo é uma aproximação física orientada a gameplay: não pretende simular fluidodinâmica ou uma linha física de alta fidelidade, mas deve manter relações coerentes entre peixe, água, linha, carreto e jogador.

## Princípios

1. A carpa e o rig estão ligados pela montagem durante a luta.
2. O jogador não move diretamente a carpa.
3. O carreto recupera linha; a tensão resultante transmite força ao peixe.
4. A carpa gera força própria e essa força é limitada pela stamina.
5. A água opõe-se ao movimento, aumentando rapidamente com a velocidade.
6. Tensão elevada consome stamina.
7. Stamina baixa reduz a força disponível, mas nunca congela o peixe.
8. Peso representa massa/inércia; não é convertido diretamente em força de tração.
9. A linha tem comprimento, elasticidade e tensão máxima.
10. Os parâmetros de equipamento devem poder alterar o resultado sem alterar a física base.

## Variáveis principais

### Peixe

- `massKg`: massa do peixe.
- `strength`: capacidade muscular relativa.
- `stamina`: energia atual para lutar.
- `maxStamina`: reserva inicial.
- `velocity`: velocidade atual.
- `preferredBurstSpeed`: velocidade de referência para arrancadas.

### Água

- `waterDragCoefficient`: resistência base da água.
- `waterDragExponent`: expoente da velocidade; valor 2 aproxima o arrasto quadrático.

Modelo conceptual:

`waterDrag = waterDragCoefficient * speed^2`

### Linha

- `lineLength`: comprimento disponível entre o jogador e o ponto ligado ao peixe.
- `comfortableLength`: distância onde a linha ainda tem pouca tensão.
- `lineElasticity`: quanto a linha tolera/absorve de alongamento.
- `maxTension`: tensão máxima antes de rebentar.
- `tension`: tensão atual.

A linha não deve ser tratada como uma mola visual. A elasticidade existe para amortecer mudanças rápidas e produzir uma subida gradual da tensão.

### Carreto

- `reelRecoverySpeed`: velocidade máxima de recuperação de linha.
- `reelDragForce`: força máxima que o carreto pode transmitir.
- `reelEfficiency`: perdas do sistema.

O `R` representa a ação de recolher linha. O carreto não teletransporta o peixe nem força diretamente a sua posição.

## Forças

A luta pode ser entendida como a interação entre:

`fishForce`

`waterDrag`

`lineTension`

`reelForce`

O movimento resultante é consequência dessas forças e da massa do peixe.

Modelo simplificado:

`netForce = fishForce - waterDrag - lineForce`

`acceleration = netForce / massKg`

A direção deve ser tratada separadamente da magnitude para permitir que a carpa mude de direção durante a luta.

## Força do peixe e stamina

A força disponível deve diminuir continuamente com a stamina.

`staminaFactor = clamp(stamina / maxStamina, 0, 1)`

`availableFishForce = baseFishForce * strength * staminaFactor`

A stamina não deve ser simplesmente drenada a uma taxa fixa durante todos os estados. O gasto depende da exigência da luta.

Uma aproximação inicial:

`staminaDrain = baseFightCost + tensionCost + movementCost`

Arrancadas acrescentam custo adicional porque aumentam velocidade, arrasto e tensão.

## Tensão

A tensão nasce da diferença entre o movimento que o peixe tenta executar e o movimento permitido pela linha/carreto.

Enquanto a linha estiver dentro da zona confortável, a tensão é baixa.

Quando a distância efetiva ultrapassa a zona confortável, a tensão aumenta progressivamente.

A tensão deve ser suavizada para evitar oscilações artificiais frame a frame.

A tensão influencia:

- força transmitida pela linha;
- gasto de stamina;
- risco de rebentar a linha;
- capacidade do pescador de recuperar linha.

## Carreto e recuperação

Quando o jogador mantém `R`, o carreto tenta recuperar linha.

Se a força da carpa superar a capacidade efetiva do carreto, o peixe continua a ganhar linha.

Se a capacidade do carreto superar a resistência efetiva do peixe, o jogador recupera linha.

Quando as forças estão próximas, a posição muda lentamente.

Isto deve evitar o comportamento artificial de o chumbo avançar sozinho enquanto a carpa fica para trás.

## Arrancadas

Uma arrancada é uma intenção de movimento forte do peixe, não uma ordem para mover instantaneamente o transform.

Durante uma arrancada:

1. a carpa tenta acelerar numa direção;
2. a massa limita a aceleração;
3. a velocidade aumenta;
4. o arrasto da água aumenta com a velocidade;
5. a tensão da linha aumenta se o peixe ganhar linha;
6. a stamina diminui em função da exigência;
7. quando a stamina baixa, a força disponível e a capacidade de aceleração diminuem.

A carpa pode continuar a mover-se depois de atingir stamina muito baixa; simplesmente já não consegue produzir a mesma força.

## Direção

A carpa não deve fazer todas as arrancadas diretamente para longe do jogador.

A direção deve poder variar entre:

- afastamento;
- lateral esquerda/direita;
- diagonal;
- mudança de direção durante a luta.

A tensão resultante muda conforme o ângulo entre a direção da carpa e a linha.

## Massa e peso

Peso e força são conceitos separados.

Uma carpa maior tem maior massa e portanto maior inércia. Isso torna aceleração e mudanças de direção mais pesadas.

O peso também pode influenciar a resistência base do peixe, mas não deve ser usado como uma conversão direta `kg = força`.

## Equipamento futuro

A física base deve permitir depois:

- linhas com diferentes resistências e elasticidades;
- carretes com diferentes drag/recovery;
- canas com diferentes ações e capacidade de absorver picos de tensão;
- hooklinks;
- anzóis e perda de peixe;
- obstáculos e vegetação;
- profundidade e correntes;
- diferentes espécies.

## Ordem de implementação

1. Substituir o movimento artificial atual por movimento baseado em velocidade/aceleração.
2. Implementar força e massa do peixe.
3. Implementar arrasto da água.
4. Implementar distância efetiva e tensão da linha.
5. Fazer o carreto aplicar força em vez de mover diretamente o peixe.
6. Ligar tensão e esforço à stamina.
7. Implementar rebentamento por tensão excessiva.
8. Adicionar direção variável e obstáculos.
9. Calibrar os parâmetros com carpas de 5, 10 e 20 kg.

## Critério de sucesso

Uma luta deve parecer diferente quando se altera peso, força, linha ou carreto sem ser necessário adicionar regras especiais para cada caso.

Se uma carpa de 10 kg estiver a fazer uma arrancada, o jogador deve sentir que está a lutar contra uma força que nasce do próprio sistema. Quando a carpa se cansar, deve ficar progressivamente mais controlável, e não simplesmente parar.
