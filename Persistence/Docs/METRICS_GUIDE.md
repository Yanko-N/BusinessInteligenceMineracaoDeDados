# Bakery Sales Predictor — Metrics & Presentation Guide

## Overview

This application uses **historical bakery sales data** (~20,000 transactions) to predict daily and per-period sales counts using a **group-average statistical model**. The model groups sales by day of week, period of day, and type of day, then computes averages to make predictions.

---

## How to Present the Application

### Step 1 — Train the Model

1. Open the **Predict** page.
2. Click **"Train Model"**. This loads the CSV data and computes all statistics.
3. A green banner shows: total records, unique days, unique products, average daily sales, and the model quality metrics (R², MAE, RMSE).

### Step 2 — Make a Prediction

1. Select a **Day of Week** (the Type of Day auto-adjusts to Weekend/Weekday).
2. Select a **Period of Day** (Morning, Afternoon, Evening, Night).
3. Click **"Predict"**.
4. The results panel shows predicted sales counts, model metrics, and the top 5 products for that period.

### Step 3 — Interpret the Results

Explain each section of the results panel to the audience (see below).

---

## What Each Metric Means

### Prediction Values

| Value | What It Shows |
|---|---|
| **Predicted Items Sold (Full Day)** | The average number of items sold on that day of the week (e.g., all Mondays). This is the total daily prediction regardless of time period. |
| **Predicted Items Sold (Period)** | The average number of items sold during the selected period (e.g., Monday mornings). More granular than the full-day number. |

### Model Quality Metrics

These measure how good the model is at explaining the variation in the data.

| Metric | Full Name | What It Means | Ideal Value |
|---|---|---|---|
| **R²** | Coefficient of Determination | How much of the variance in daily sales the model explains. **1.0** = perfect fit, **0.0** = no better than guessing the overall average. Values above **0.5** mean the model captures meaningful patterns. | As close to **1.0** as possible |
| **MAE** | Mean Absolute Error | The average difference (in items) between what the model predicted and what actually happened. A MAE of **10** means predictions are off by about 10 items on average. | As close to **0** as possible |
| **RMSE** | Root Mean Square Error | Similar to MAE but penalizes large errors more. If RMSE is much larger than MAE, it means there are some days with large prediction errors. | As close to **0** as possible |

#### How to explain R² in plain language:
> "Our model's R² is 0.35. This means that 35% of the variation in daily sales can be explained by knowing what day of the week it is and whether it's a weekday or weekend. The remaining 65% is due to other factors like weather, holidays, or promotions."

#### How to explain MAE:
> "The MAE is 15.2. This means on average, our prediction is off by about 15 items per day."

#### How to explain RMSE:
> "The RMSE is 19.8. This is the 'typical' error size. It's a bit higher than MAE because some days have larger miss-predictions, which RMSE captures."

### Prediction Detail Metrics

| Metric | What It Means |
|---|---|
| **Confidence Score (%)** | A 0–100% score indicating how reliable this specific prediction is. Higher confidence means: more historical data for this combination AND lower variance (more consistent sales). A score of **80%+** is strong; below **40%** means limited data or high variability. |
| **Sample Count** | The number of distinct days in the dataset that match this exact combination (e.g., how many Monday mornings on weekdays exist in the data). More samples = more reliable prediction. |
| **Standard Deviation (±)** | How much the actual sales counts varied around the average for this combination. A low std. dev. means sales are very consistent; a high one means they fluctuate a lot day to day. |

#### How to explain Confidence:
> "The confidence for Monday mornings is 72%. We had 12 Monday mornings in our data and the sales were fairly consistent. We can trust this prediction reasonably well."

#### How to explain Standard Deviation:
> "The ± means our prediction of 25 items could realistically range from about 20 to 30 items on any given day."

### Top 5 Products

| Column | What It Shows |
|---|---|
| **Product** | Name of the bakery item (e.g., Coffee, Bread, Pastry). |
| **Sales Count** | How many times this product was sold in this exact combination across the entire dataset. |
| **Share (%)** | What percentage of all sales in this combination belong to this product. Helps identify the dominant items to stock for that period. |

---

## Presentation Tips

1. **Start with the business question**: "How many items will we sell on a Monday morning, and which products should we stock?"
2. **Show the training step**: Emphasize the ~20,000 real transactions being processed.
3. **Compare weekday vs. weekend**: Run predictions for both to show the difference. Weekends may have completely different top products.
4. **Highlight actionable insights**: "On Saturday afternoons, Coffee accounts for 30% of sales — we should make sure we have extra stock."
5. **Discuss model limitations**: R² shows the model doesn't capture everything (weather, holidays, events). MAE shows the typical error margin. This is honest and adds credibility.
6. **Show confidence variation**: Pick a high-confidence prediction and a low-confidence one to demonstrate that the model knows when it's less sure.

---

## Glossary

| Term | Definition |
|---|---|
| **R² (R-Squared)** | Statistical measure (0 to 1) of how well the model fits data. Higher is better. |
| **MAE** | Average prediction error in absolute terms. |
| **RMSE** | Like MAE but more sensitive to outlier errors. |
| **Standard Deviation** | Spread of values around the mean — measures consistency. |
| **Confidence Score** | Custom 0–100% metric combining sample size and consistency. |
| **Group-Average Model** | Prediction approach that uses the historical average of matching groups (same day + period + type) as the predicted value. |
| **Weekday** | Monday through Friday. |
| **Weekend** | Saturday and Sunday. |
| **Period of Day** | Morning, Afternoon, Evening, or Night — time slots for sales analysis. |
