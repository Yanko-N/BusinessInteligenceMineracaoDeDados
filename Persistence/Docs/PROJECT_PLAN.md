# Business Intelligence & Data Mining — Project Plan

## 1. Dataset Overview

**Source:** `bakery_sales_revised.csv`

| Property        | Value                                |
|-----------------|--------------------------------------|
| Total Records   | 20,507 transaction rows              |
| Unique Products | 94 items (Bread, Coffee, Pastry ...) |
| Date Range      | Oct 30, 2016 – Apr 9, 2017          |
| Features        | Transaction, Item, date_time, period_day, weekday_weekend |

### Feature Dictionary

| Column            | Type        | Values / Description                          |
|-------------------|-------------|-----------------------------------------------|
| `Transaction`     | int         | Groups items belonging to the same purchase   |
| `Item`            | string      | Product name (94 distinct values)             |
| `date_time`       | datetime    | Timestamp of the transaction                  |
| `period_day`      | categorical | morning, afternoon, evening, night            |
| `weekday_weekend` | categorical | weekday, weekend                              |

### Derived Features (to be engineered)

| Feature               | Derivation                                           |
|------------------------|------------------------------------------------------|
| `HourOfDay`            | Extract hour from `date_time`                       |
| `DayOfWeek`            | 0 = Monday … 6 = Sunday                            |
| `Month`                | Extract month from `date_time`                      |
| `ItemsPerTransaction`  | Count of rows sharing the same `Transaction` id     |
| `DailySalesCount`      | Total transactions per calendar date                |
| `ItemFrequency`        | How often each item appears in the full dataset     |

---

## 2. Business Questions to Answer

### 2.1 Regression — "How many?"

> **Goal:** Predict a continuous numeric value.

| # | Question | Target Variable | Model |
|---|----------|-----------------|-------|
| R1 | **How many total items will be sold on a given day?** | `DailySalesCount` (items/day) | Regression (FastTree / SDCA / AutoML) |
| R2 | **How many transactions will occur in a specific time period (morning/afternoon/evening)?** | `PeriodTransactionCount` | Regression |
| R3 | **How many items per transaction on average for a given day type?** | `ItemsPerTransaction` | Regression |

**Input features:** DayOfWeek, Month, PeriodDay, TypeOfDay, HourOfDay.

**Metrics:** R², MAE, RMSE.

---

### 2.2 Classification — "What / Which?"

> **Goal:** Predict a discrete category / label.

| # | Question | Target Label | Model |
|---|----------|--------------|-------|
| C1 | **What product is most likely to be purchased given the time of day and day type?** | `Item` (multi-class) | Multi-class Classification (LightGBM / FastTree) |
| C2 | **Is a transaction happening on a weekday or weekend?** Given only the hour, item mix, and period. | `weekday_weekend` (binary) | Binary Classification (Logistic Regression / FastTree) |
| C3 | **Which period of the day does a transaction belong to?** Given hour, item, and day type. | `period_day` (multi-class) | Multi-class Classification |

**Input features:** ItemEncoded, HourOfDay, DayOfWeek, Month, TypeOfDay / PeriodDay (depending on target).

**Metrics:** Accuracy, Macro-F1, Log-Loss, Confusion Matrix.

---

### 2.3 Forecasting (Time-Series) — "How will it evolve?"

> **Goal:** Predict future values based on historical time-series patterns.

| # | Question | Target Series | Model |
|---|----------|---------------|-------|
| F1 | **What will daily sales volume look like over the next 7 / 14 / 30 days?** | `DailySalesCount` over time | SSA (Singular Spectrum Analysis) / ML.NET Forecasting |
| F2 | **What is the expected trend for a specific top product (e.g., Coffee, Bread) over the next weeks?** | `DailyItemCount` per product | SSA Forecasting |

**Input:** Time-indexed daily aggregation of sales counts.

**Metrics:** MAE, RMSE, MAPE, visual trend chart.

---

## 3. ML Models & Algorithms (ML.NET)

| Task             | Algorithm(s)                                      | ML.NET API                                |
|------------------|---------------------------------------------------|-------------------------------------------|
| Regression       | FastTree, SDCA, LightGBM, AutoML Regression       | `RegressionExperiment` / Trainers         |
| Binary Classif.  | FastTree, Logistic Regression, AutoML Binary       | `BinaryClassificationExperiment`          |
| Multi-class      | LightGBM, FastTree, SDCA (MaximumEntropy), AutoML | `MulticlassClassificationExperiment`      |
| Forecasting      | SSA (Singular Spectrum Analysis)                   | `ForecastBySsa`                           |

---

## 4. Website Pages & Features

| Page / Section             | Purpose                                                        | Models Used     |
|----------------------------|----------------------------------------------------------------|-----------------|
| **Dashboard**              | Overview charts — daily sales, top products, period breakdown  | —               |
| **Regression Predictor**   | Form: select day type, period, day of week → predicts sales count | R1, R2, R3   |
| **Classification Predictor** | Form: input time/period/day → predicts most likely product or day type | C1, C2, C3 |
| **Forecasting**            | Interactive chart showing predicted sales for the next N days  | F1, F2          |
| **Data Explorer**          | Table/grid with filtering/sorting of the raw bakery sales data | —               |

---

## 5. Architecture Overview

```
┌─────────────────────────────────────────────────┐
│              BusinessInteligence                │
│            (ASP.NET Core MVC Web)               │
│  Controllers ─► Views (Razor + Chart.js)        │
└────────────────────┬────────────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────────────┐
│                Application                      │
│  Mediator ─► Request Handlers                   │
│  Features/                                      │
│    Sales/                                       │
│      Commands/ (CreateSaleCommand)              │
│      Queries/  (GetAllSalesQuery)               │
│    Predictions/                                 │
│      Regression/   (PredictSalesCount)          │
│      Classification/ (PredictProduct,           │
│                       PredictDayType)           │
│      Forecasting/  (ForecastDailySales)         │
│  Core/                                          │
│    SalesTrainer   (train & persist models)      │
│    ML.NET MLContext                             │
└────────────────────┬────────────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────────────┐
│                Persistence                      │
│  Models/  (Sale)                                │
│  Enums/   (PeriodDay, TypeOfDay)                │
│  Docs/    (CSV, this plan)                      │
└─────────────────────────────────────────────────┘
```

---

## 6. Implementation Roadmap

### Phase 1 — Data & Infrastructure
- [x] CSV loaded and Sale model created
- [x] Mediator pattern set up
- [ ] CSV parsing service (read CSV → `List<Sale>`)
- [ ] Feature engineering pipeline (derived columns)

### Phase 2 — Model Training
- [ ] **Regression trainer** — predict daily sales count
- [ ] **Multi-class classifier** — predict product given context
- [ ] **Binary classifier** — predict weekday vs. weekend
- [ ] **Forecasting trainer** — SSA on daily aggregated series
- [ ] Model persistence (save/load `.zip` model files)

### Phase 3 — Prediction Handlers
- [ ] `PredictSalesCountHandler` (Regression)
- [ ] `PredictProductHandler` (Classification)
- [ ] `PredictDayTypeHandler` (Binary Classification)
- [ ] `ForecastDailySalesHandler` (Forecasting)

### Phase 4 — Web UI
- [ ] Dashboard page with summary charts (Chart.js)
- [ ] Regression prediction form + results
- [ ] Classification prediction form + results
- [ ] Forecasting chart (interactive date range)
- [ ] Data explorer page (filterable table)

### Phase 5 — Polish
- [ ] Model evaluation metrics display
- [ ] Error handling and loading states
- [ ] Responsive layout

---

## 7. Summary Table

| Category        | Question                                                 | Model Type          | Algorithm           |
|-----------------|----------------------------------------------------------|---------------------|---------------------|
| **Regression**  | How many items will be sold on a given day?              | Regression          | FastTree / AutoML   |
| **Regression**  | How many transactions per time period?                   | Regression          | SDCA / AutoML       |
| **Regression**  | Average items per transaction for a day type?            | Regression          | LightGBM / AutoML   |
| **Classification** | What product is most likely at a given time?          | Multi-class Classif.| LightGBM / FastTree |
| **Classification** | Is the transaction on a weekday or weekend?           | Binary Classif.     | Logistic / FastTree |
| **Classification** | Which period of day does the transaction belong to?   | Multi-class Classif.| SDCA / FastTree     |
| **Forecasting** | Daily sales over the next 7/14/30 days?                 | Time-Series         | SSA                 |
| **Forecasting** | Trend for a specific product over coming weeks?          | Time-Series         | SSA                 |
