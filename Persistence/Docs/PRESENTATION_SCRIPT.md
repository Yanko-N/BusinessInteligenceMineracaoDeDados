# Guião da Apresentação Business Intelligence & Mineração de Dados

---

## Slide 1 Título

- **Título:** Business Intelligence & Mineração de Dados Análise de Vendas de Padaria
- **Subtítulo:** Regressão, Classificação e Previsão com ML.NET
- **Rodapé:** Nome, universidade, cadeira, data

---

## Slide 2 Agenda

1. Visão Geral do Dataset
2. Metodologia CRISP-DM
3. Exploração de Dados e Engenharia de Features
4. Modelos de Regressão
5. Modelos de Classificação
6. Modelos de Previsão (Forecasting)
7. Demonstração da Aplicação Web
8. Resultados e Conclusões

---

## Slide 3 Visão Geral do Dataset

- **Fonte:** Transações de vendas de uma padaria (Out 2016 – Abr 2017)
- **20.507 linhas**, **94 produtos únicos**
- **5 colunas:** Transaction, Item, date_time, period_day, weekday_weekend
- Mostrar uma pequena tabela de exemplo (5–6 linhas do CSV)
- Mencionar: cada linha = um item numa venda; várias linhas podem partilhar o mesmo ID de venda

---

## Slide 4 Metodologia: CRISP-DM

> Seguimos o **CRISP-DM** (Cross-Industry Standard Process for Data Mining), a metodologia padrão da indústria para projetos de mineração de dados.

Mostrar o diagrama do ciclo de 6 fases:

1. **Compreensão do Negócio** Definir as questões de negócio (quantas vendas? que produto? tendência futura?)
2. **Compreensão dos Dados** Explorar o CSV da padaria: distribuições, produtos mais vendidos, padrões temporais
3. **Preparação dos Dados** Limpar dados, criar features derivadas (HourOfDay, DayOfWeek, Month, ItemsPerTransaction, DailySalesCount)
4. **Modelação** Treinar modelos de Regressão, Classificação e Previsão usando ML.NET
5. **Avaliação** Medir R², Accuracy, F1, MAE; comparar algoritmos via AutoML
6. **Implementação** Aplicação web ASP.NET Core MVC que serve previsões aos utilizadores finais

**Porquê CRISP-DM:** É iterativo podemos voltar atrás e refinar features ou experimentar novos algoritmos em qualquer fase.

---

## Slide 5 Exploração de Dados

- **Gráfico de barras:** Top 10 produtos mais vendidos (Coffee, Bread, Tea, Pastry, Cake …)
- **Gráfico circular:** Vendas por período do dia (manhã domina)
- **Gráfico de barras:** Dia de semana vs. fim de semana
- **Gráfico de linhas:** Contagem diária de vendas ao longo do tempo (Out 2016 – Abr 2017) mostrando sazonalidade semanal

---

## Slide 6 Engenharia de Features

| Feature Original   | Feature Derivada    | Objetivo            |
|----------------------|-------------------------|---------------------------------|
| `date_time`     | `HourOfDay`       | Captar padrões horários     |
| `date_time`     | `DayOfWeek` (0–6)    | Captar padrões semanais     |
| `date_time`     | `Month`         | Captar sazonalidade mensal   |
| `Transaction`    | `ItemsPerTransaction`  | Tamanho do cesto de compras   |
| todas as linhas/data | `DailySalesCount`    | Alvo para regressão/previsão  |
| `Item`        | `ItemEncoded` (numérico)| Codificação numérica para ML  |

---

## Slide 7 Resumo das Questões de Negócio

| Categoria    | Questão                            | Tipo de Modelo    |
|------------------|----------------------------------------------------------------|----------------------|
| Regressão    | Quantos itens serão vendidos num dado dia?           | Regressão      |
| Regressão    | Quantas vendas por período do dia?             | Regressão      |
| Classificação  | Qual o produto mais provável a uma dada hora?         | Multi-classe     |
| Classificação  | A venda é num dia de semana ou fim de semana?       | Binária       |
| Classificação  | A que período do dia pertence a venda?           | Multi-classe     |
| Previsão     | Como serão as vendas diárias nos próximos 7–30 dias?      | Séries temporais (SSA)|
| Previsão     | Tendência de um produto específico nas próximas semanas?    | Séries temporais (SSA)|

---

## Slide 8 Modelos de Regressão

- **Objetivo:** Prever valores contínuos (contagem diária de vendas, vendas por período)
- **Algoritmos:** FastTree, SDCA, LightGBM (selecionados via AutoML)
- **Pipeline:** Features → Concatenar → Normalizar → Treinar → Avaliar
- **Input:** DayOfWeek, Month, PeriodDay, TypeOfDay, HourOfDay
- **Output:** Número previsto (ex.: "Terça manhã → ~120 itens")
- **Métricas:** R², MAE, RMSE
- Mostrar gráfico de dispersão: Previsto vs. Real

---

## Slide 9 Modelos de Classificação

- **Objetivo:** Prever categorias
- **Classificação Binária** Dia de semana vs. Fim de semana
 - Algoritmos: Regressão Logística, FastTree
 - Métricas: Accuracy, AUC, F1
 - Mostrar matriz de confusão
- **Classificação Multi-classe** Prever produto / período do dia
 - Algoritmos: LightGBM, FastTree, MaximumEntropy
 - Métricas: Macro-Accuracy, Macro-F1, Log-Loss
 - Mostrar top-5 produtos previstos com grau de confiança

---

## Slide 10 Modelos de Previsão (Forecasting)

- **Objetivo:** Prever o volume diário de vendas futuras
- **Algoritmo:** SSA (Singular Spectrum Analysis) via ML.NET `ForecastBySsa`
- **Como funciona o SSA (resumo):**
 - Decompõe a série temporal em tendência + sazonalidade + ruído
 - Usa uma janela deslizante para captar padrões repetitivos
 - Projeta para a frente para prever valores futuros
- **Input:** Vendas diárias históricas agregadas por data
- **Output:** Valores previstos + intervalos de confiança para os próximos N dias
- Mostrar gráfico de linhas: dados históricos → previsão com limites superior/inferior

---

## Slide 11 Tecnologia e Arquitetura

- **Stack:** C# / .NET 8, ASP.NET Core MVC, ML.NET, Chart.js
- Mostrar o diagrama de arquitetura em 3 camadas:
 - **Web** (BusinessInteligence) Controllers, Razor Views, Gráficos
 - **Application** Padrão Mediator, Trainers, Handlers de Previsão
 - **Persistence** Modelos, Enums, dados CSV
- Mencionar: O padrão Mediator desacopla o tratamento de pedidos dos controladores

---

## Slide 12 Páginas da Aplicação Web

| Página           | O que mostra                          |
|----------------------------|-----------------------------------------------------------------|
| **Dashboard**       | Gráficos resumo: Top produtos, tendências diárias, por período |
| **Previsão por Regressão** | Formulário → selecionar dia/período → obter contagem prevista  |
| **Classificação**     | Formulário → inserir hora/contexto → produto ou tipo de dia previsto |
| **Forecasting**      | Gráfico interativo com previsão de vendas futuras        |
| **Explorador de Dados**  | Tabela pesquisável/filtrável dos dados brutos de vendas     |

Incluir capturas de ecrã ou mockups se disponíveis.

---

## Slide 13 Resultados e Avaliação

- Tabela comparativa do desempenho dos modelos:

| Modelo           | Melhor Algoritmo | Métrica Principal  | Valor    |
|----------------------------|------------------|----------------------|-------------|
| Regressão Vendas Diárias  | (via AutoML)   | R²          | (preencher) |
| Classificação de Produto  | (via AutoML)   | Macro-Accuracy    | (preencher) |
| Binária Semana/Fim-de-semana| (via AutoML)  | AUC         | (preencher) |
| Previsão de Vendas     | SSA       | MAE         | (preencher) |

- Pontos-chave / insights encontrados

---

## Slide 14 Conclusões

- O CRISP-DM forneceu uma abordagem estruturada e iterativa para todo o pipeline
- O ML.NET AutoML simplifica a seleção de algoritmos comparámos múltiplos algoritmos automaticamente
- A padaria beneficia mais da **previsão** (planeamento de stock) e da **classificação** (ter os produtos certos na hora certa)
- A aplicação web torna as previsões acessíveis a funcionários sem conhecimentos técnicos

---

## Slide 15 Trabalho Futuro

- Adicionar dados de preço para previsão de receita (não apenas quantidade)
- Análise de cesto de compras (regras de associação: "clientes que compram X também compram Y")
- Pipeline de previsão em tempo real com dados POS ao vivo
- Testes A/B de promoções com base nos insights de classificação

---

## Slide 16 Perguntas

- **Dúvidas?**
- Obrigado
