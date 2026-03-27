# Bakery Sales Intelligence & Data Mining with ML Prediction

An ASP.NET Core MVC application that applies machine learning (ML.NET) to historical bakery sales data, enabling sales prediction, product classification, day-type classification, and time-series forecasting.

## Features

- **Daily Sales Prediction** : Predict total items sold on a given day using FastTree Regression.
- **Product Classification** : Predict which product will be sold given time/context using SDCA MaxEntropy.
- **Day Type Classification** : Classify a day as weekday or weekend based on sales patterns using FastTree Binary Classifier.
- **Sales Forecasting** : Forecast daily sales for 7, 14, or 30 days ahead using Singular Spectrum Analysis (SSA).
- **In-app Model Training** : Train all models directly from the UI with an 80/20 train-test split.
- **Performance Metrics** : View R², MAE, RMSE, Accuracy, LogLoss, AUC, and F1 Score after training.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Web Framework | ASP.NET Core MVC (.NET 8) |
| Machine Learning | Microsoft.ML 5.0, Microsoft.ML.AutoML 0.23, Microsoft.ML.TimeSeries 5.0 |
| Frontend | Razor Views, Bootstrap 5 |
| Architecture | Custom CQRS (Mediator pattern), Dependency Injection |

## Solution Structure

```
BusinessInteligenceMineracaoDeDados/
├── BusinessInteligence/        # ASP.NET Core MVC — Controllers, Views, DI setup
├── Application/                # Business logic — ML engine, mediator, features, models
└── Persistence/                # Domain entities, enums, CSV dataset
```

| Project | Role |
|---------|------|
| **BusinessInteligence** | UI layer — Razor views, `HomeController`, service configuration |
| **Application** | ML training & prediction engine (`SalesTrainer`), CQRS features (commands/queries), custom mediator |
| **Persistence** | Domain models (`Sale`), enums (`PeriodDay`, `TypeOfDay`), CSV dataset |

## ML Models

| Model | Algorithm | Purpose | Key Metrics |
|-------|-----------|---------|-------------|
| Regression | FastTree Regression | Predict daily sales volume | R², MAE, RMSE |
| Multi-class Classification | SDCA MaxEntropy | Predict which product is sold | Accuracy, LogLoss |
| Binary Classification | FastTree Binary | Classify weekday vs weekend | Accuracy, AUC, F1 |
| Time-Series Forecasting | SSA | Forecast future daily sales | Point forecast + 95% confidence intervals |

## Dataset

The application uses `Persistence/Docs/bakery_sales_revised.csv` containing ~20,000 historical bakery sales records with the following columns:

| Column | Description |
|--------|-------------|
| Transaction | Transaction ID |
| Item | Product name (Bread, Coffee, Muffin, Pastry, etc.) |
| date_time | Timestamp of sale |
| period_day | Time period (morning, afternoon, evening, night) |
| weekday_weekend | Day type (weekday, weekend) |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
# Clone the repository
git clone <repository-url>
cd BusinessInteligenceMineracaoDeDados

# Restore dependencies
dotnet restore

# Run the web application
dotnet run --project BusinessInteligence
```

The app will be available at `https://localhost:5001` (or the port configured in `launchSettings.json`).

### Usage

1. **Train Models** : On the home page, click "Train Models" to train all ML models from the CSV dataset.
2. **Predict** : Navigate to the Predict page, select a day of week, month, period, and day type to get a daily sales prediction.
3. **Classify** : Navigate to the Classify page to predict which product will be sold or classify the day type.
4. **Forecast** : Navigate to the Forecast page and choose a horizon (7, 14, or 30 days) to see future sales projections with confidence intervals.

## Architecture

The application uses a **custom CQRS pattern** with a hand-built mediator (no MediatR dependency):

- **Commands**: `TrainSalesPredictionCommand` wich trains models from CSV data.
- **Queries**: `PredictDailySalesQuery`, `PredictProductQuery`, `PredictDayTypeQuery`, `ForecastSalesQuery`, `UniqueProductsQuery`.
- **SalesTrainer**: Central ML engine that manages all four models with thread-safe prediction via locks.
- **Result\<T\>**: Generic wrapper for success/failure responses throughout the application.
