import os
import warnings

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import seaborn as sns
from plotly.subplots import make_subplots

warnings.filterwarnings("ignore")
sns.set_theme(style="whitegrid", palette="muted", font_scale=1.1)

# 1. Load Data
CSV_PATH = os.path.join(
    os.path.dirname(__file__), "..",  "Persistence", "Docs", "bakery_sales_revised.csv"
)
EXPORT_DIR = os.path.join(
    os.path.dirname(__file__), "..", "BusinessInteligence", "wwwroot", "charts"
)
os.makedirs(EXPORT_DIR, exist_ok=True)

df = pd.read_csv(CSV_PATH)

# 2. Basic Exploration
print("=" * 60)
print("SHAPE:", df.shape)
print("=" * 60)
print("\nFIRST 5 ROWS:")
print(df.head())
print("\nDATA TYPES:")
print(df.dtypes)
print("\nBASIC STATISTICS:")
print(df.describe(include="all"))
print("\nMISSING VALUES:")
print(df.isnull().sum())
print("\nDUPLICATED ROWS:", df.duplicated().sum())
print("\nUNIQUE ITEMS:", df["Item"].nunique())

# 3. Data Preprocessing
df["date_time"] = pd.to_datetime(df["date_time"], format="%m/%d/%Y %H:%M")
df["date"] = df["date_time"].dt.date
df["month"] = df["date_time"].dt.month
df["month_name"] = df["date_time"].dt.month_name()
df["day_of_week"] = df["date_time"].dt.day_name()
df["hour"] = df["date_time"].dt.hour
df["year_month"] = df["date_time"].dt.to_period("M").astype(str)

print("\nDATE RANGE:", df["date_time"].min(), "->", df["date_time"].max())

# Helper to save figures
def save(fig, name: str, *, plotly_fig=False):
    path = os.path.join(EXPORT_DIR, name)
    if plotly_fig:
        fig.write_html(path.replace(".png", ".html"))
        print(f"  [ DONE ] saved {name.replace('.png', '.html')}")
    else:
        fig.savefig(path, dpi=150, bbox_inches="tight")
        plt.close(fig)
        print(f"  [ DONE ] saved {name}")


# 4. UNIVARIATE ANALYSIS
print("\n" + "=" * 60)
print("UNIVARIATE ANALYSIS")
print("=" * 60)

# 4a. Top 15 items by frequency
top_items = df["Item"].value_counts().head(15)

fig, ax = plt.subplots(figsize=(12, 6))
colors = sns.color_palette("viridis", len(top_items))
bars = ax.barh(top_items.index[::-1], top_items.values[::-1], color=colors)
ax.bar_label(bars, padding=3)
ax.set_title("Top 15 Best-Selling Items", fontsize=16, fontweight="bold")
ax.set_xlabel("Number of Sales")
save(fig, "01_top15_items.png")

# 4b. Pie chart – Top 10 Items share
top10 = df["Item"].value_counts().head(10)
other = df["Item"].value_counts().iloc[10:].sum()
pie_data = pd.concat([top10, pd.Series({"Others": other})])

fig_pie = px.pie(
    names=pie_data.index,
    values=pie_data.values,
    title="Sales Distribution – Top 10 Items vs Others",
    hole=0.4,
    color_discrete_sequence=px.colors.qualitative.Set3,
)
fig_pie.update_traces(textposition="inside", textinfo="percent+label")
save(fig_pie, "02_pie_top10.png", plotly_fig=True)

# 4c. Period of Day distribution 
period_order = ["morning", "afternoon", "evening", "night"]
period_counts = df["period_day"].value_counts().reindex(period_order).dropna()

fig, ax = plt.subplots(figsize=(8, 5))
palette = {"morning": "#FFD700", "afternoon": "#FF8C00", "evening": "#4B0082", "night": "#191970"}
bars = ax.bar(period_counts.index, period_counts.values,
              color=[palette.get(p, "#888") for p in period_counts.index])
ax.bar_label(bars, padding=3, fontsize=11)
ax.set_title("Sales by Period of Day", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
save(fig, "03_period_of_day.png")

# 4d. Weekday vs Weekend
ww_counts = df["weekday_weekend"].value_counts()

fig, ax = plt.subplots(figsize=(6, 5))
bars = ax.bar(ww_counts.index, ww_counts.values, color=["#3498db", "#e74c3c"], width=0.5)
ax.bar_label(bars, padding=3, fontsize=12)
ax.set_title("Sales: Weekday vs Weekend", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
save(fig, "04_weekday_weekend.png")


# 5. BIVARIATE / MULTIVARIATE ANALYSIS
print("\n" + "=" * 60)
print("BIVARIATE / MULTIVARIATE ANALYSIS")
print("=" * 60)

# 5a. Heatmap – Items x Period of Day
top_items_list = df["Item"].value_counts().head(12).index.tolist()
heat_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "period_day"])
    .size()
    .unstack(fill_value=0)
    .reindex(columns=[p for p in period_order if p in df["period_day"].unique()])
)

fig, ax = plt.subplots(figsize=(10, 7))
sns.heatmap(heat_data, annot=True, fmt="d", cmap="YlOrRd", linewidths=0.5, ax=ax)
ax.set_title("Top 12 Items x Period of Day", fontsize=16, fontweight="bold")
save(fig, "05_heatmap_item_period.png")

# 5b. Heatmap – Items x Day of Week
day_order = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
heat_day = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "day_of_week"])
    .size()
    .unstack(fill_value=0)
    .reindex(columns=day_order)
)

fig, ax = plt.subplots(figsize=(12, 7))
sns.heatmap(heat_day, annot=True, fmt="d", cmap="coolwarm", linewidths=0.5, ax=ax)
ax.set_title("Top 12 Items x Day of Week", fontsize=16, fontweight="bold")
save(fig, "06_heatmap_item_dayofweek.png")

# 5c. Stacked bar – Period x Weekday/Weekend
stacked = (
    df.groupby(["period_day", "weekday_weekend"])
    .size()
    .unstack(fill_value=0)
    .reindex([p for p in period_order if p in df["period_day"].unique()])
)

fig, ax = plt.subplots(figsize=(9, 6))
stacked.plot(kind="bar", stacked=True, color=["#3498db", "#e74c3c"], ax=ax)
ax.set_title("Period of Day – Weekday vs Weekend", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
ax.set_xticklabels(ax.get_xticklabels(), rotation=0)
ax.legend(title="Day Type")
save(fig, "07_stacked_period_ww.png")


# 6. TIME-SERIES ANALYSIS
print("\n" + "=" * 60)
print("TIME-SERIES ANALYSIS")
print("=" * 60)

# 6a. Daily transaction count
daily = df.groupby("date")["Transaction"].nunique().reset_index()
daily.columns = ["date", "transactions"]
daily["date"] = pd.to_datetime(daily["date"])

fig_ts = px.line(
    daily, x="date", y="transactions",
    title="Daily Unique Transactions Over Time",
    labels={"date": "Date", "transactions": "Unique Transactions"},
)
fig_ts.update_traces(line=dict(width=1.5))
fig_ts.update_layout(hovermode="x unified")
save(fig_ts, "08_daily_transactions.png", plotly_fig=True)

# 6b. Monthly sales volume
monthly = df.groupby("year_month").size().reset_index(name="sales")

fig, ax = plt.subplots(figsize=(12, 5))
ax.fill_between(monthly["year_month"], monthly["sales"], alpha=0.3, color="#2ecc71")
ax.plot(monthly["year_month"], monthly["sales"], marker="o", color="#27ae60")
ax.set_title("Monthly Sales Volume", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Items Sold")
ax.set_xlabel("Month")
plt.xticks(rotation=45, ha="right")
save(fig, "09_monthly_sales.png")

# 6c. Hourly sales distribution
hourly = df.groupby("hour").size()

fig, ax = plt.subplots(figsize=(12, 5))
ax.bar(hourly.index, hourly.values, color=sns.color_palette("rocket", len(hourly)), edgecolor="white")
ax.set_title("Sales Distribution by Hour", fontsize=16, fontweight="bold")
ax.set_xlabel("Hour of Day")
ax.set_ylabel("Number of Sales")
ax.set_xticks(hourly.index)
save(fig, "10_hourly_distribution.png")


# 7. ADVANCED VISUALIZATIONS

print("\n" + "=" * 60)
print("ADVANCED VISUALIZATIONS")
print("=" * 60)

# 7a. Treemap – Item x Period 
tree_df = (
    df.groupby(["period_day", "Item"])
    .size()
    .reset_index(name="count")
    .sort_values("count", ascending=False)
)

fig_tree = px.treemap(
    tree_df, path=["period_day", "Item"], values="count",
    title="Treemap: Items Sold per Period of Day",
    color="count", color_continuous_scale="Viridis",
)
save(fig_tree, "11_treemap.png", plotly_fig=True)

# 7b. Sunburst – Weekday/Weekend -> Period -> Top items 
sun_df = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["weekday_weekend", "period_day", "Item"])
    .size()
    .reset_index(name="count")
)

fig_sun = px.sunburst(
    sun_df,
    path=["weekday_weekend", "period_day", "Item"],
    values="count",
    title="Sunburst: Day Type -> Period -> Item",
    color="count",
    color_continuous_scale="RdBu",
)
save(fig_sun, "12_sunburst.png", plotly_fig=True)

# 7c. Bubble chart – Hour x Day of Week (size = sales count) 
bubble = (
    df.groupby(["day_of_week", "hour"])
    .size()
    .reset_index(name="count")
)
bubble["day_of_week"] = pd.Categorical(bubble["day_of_week"], categories=day_order, ordered=True)

fig_bubble = px.scatter(
    bubble, x="hour", y="day_of_week", size="count", color="count",
    title="Bubble Chart: Sales by Hour x Day of Week",
    labels={"hour": "Hour", "day_of_week": "Day of Week", "count": "Sales"},
    color_continuous_scale="YlOrRd",
    size_max=30,
)
fig_bubble.update_layout(yaxis=dict(categoryorder="array", categoryarray=day_order))
save(fig_bubble, "13_bubble_hour_day.png", plotly_fig=True)

# 7d. Violin plot – Hour distribution per period
fig, ax = plt.subplots(figsize=(10, 6))
order = [p for p in period_order if p in df["period_day"].unique()]
sns.violinplot(data=df, x="period_day", y="hour", order=order, palette="Set2", ax=ax)
ax.set_title("Hour Distribution per Period of Day", fontsize=16, fontweight="bold")
save(fig, "14_violin_hour_period.png")

# 7e. Monthly trend for top-5 items (small multiples)
top5 = df["Item"].value_counts().head(5).index.tolist()
top5_monthly = (
    df[df["Item"].isin(top5)]
    .groupby(["year_month", "Item"])
    .size()
    .reset_index(name="count")
)

fig_facet = px.line(
    top5_monthly, x="year_month", y="count", color="Item",
    facet_col="Item", facet_col_wrap=3,
    title="Monthly Trend – Top 5 Items",
    labels={"year_month": "Month", "count": "Sales"},
)
fig_facet.update_xaxes(tickangle=45)
fig_facet.for_each_annotation(lambda a: a.update(text=a.text.split("=")[-1]))
save(fig_facet, "15_top5_monthly_facet.png", plotly_fig=True)

# 7f. Day of week bar per weekday/weekend 
dow_ww = (
    df.groupby(["day_of_week", "weekday_weekend"])
    .size()
    .reset_index(name="count")
)
dow_ww["day_of_week"] = pd.Categorical(dow_ww["day_of_week"], categories=day_order, ordered=True)
dow_ww.sort_values("day_of_week", inplace=True)

fig_dow = px.bar(
    dow_ww, x="day_of_week", y="count", color="weekday_weekend",
    title="Sales by Day of Week",
    labels={"day_of_week": "Day", "count": "Sales", "weekday_weekend": "Type"},
    color_discrete_map={"weekday": "#3498db", "weekend": "#e74c3c"},
    barmode="group",
)
save(fig_dow, "16_day_of_week_bar.png", plotly_fig=True)

# 7g. Correlation-style heatmap – transactions per item per hour
corr_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "hour"])
    .size()
    .unstack(fill_value=0)
)

fig, ax = plt.subplots(figsize=(14, 8))
sns.heatmap(corr_data, cmap="magma_r", annot=True, fmt="d", linewidths=0.3, ax=ax)
ax.set_title("Top Items x Hour of Day", fontsize=16, fontweight="bold")
ax.set_xlabel("Hour")
save(fig, "17_heatmap_item_hour.png")

# 7h. Pair-wise transaction size analysis
txn_size = df.groupby("Transaction").agg(
    items_count=("Item", "count"),
    hour=("hour", "first"),
    period=("period_day", "first"),
    day_type=("weekday_weekend", "first"),
).reset_index()

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

sns.histplot(txn_size["items_count"], bins=range(1, txn_size["items_count"].max() + 2),
             ax=axes[0], color="#9b59b6", edgecolor="white")
axes[0].set_title("Items per Transaction", fontweight="bold")
axes[0].set_xlabel("Number of Items")

sns.boxplot(data=txn_size, x="period", y="items_count", order=order,
            palette="Set2", ax=axes[1])
axes[1].set_title("Transaction Size by Period", fontweight="bold")
axes[1].set_xlabel("Period")
axes[1].set_ylabel("Items per Transaction")

sns.boxplot(data=txn_size, x="day_type", y="items_count",
            palette=["#3498db", "#e74c3c"], ax=axes[2])
axes[2].set_title("Transaction Size: Weekday vs Weekend", fontweight="bold")
axes[2].set_xlabel("Day Type")
axes[2].set_ylabel("Items per Transaction")

fig.suptitle("Transaction Size Analysis", fontsize=18, fontweight="bold", y=1.02)
fig.tight_layout()
save(fig, "18_transaction_size.png")


# 8. SUMMARY TABLE
print("\n" + "=" * 60)
print("SUMMARY")
print("=" * 60)
print(f"  Total rows ............. {len(df):,}")
print(f"  Unique transactions .... {df['Transaction'].nunique():,}")
print(f"  Unique items ........... {df['Item'].nunique()}")
print(f"  Date range ............. {df['date_time'].min().date()} -> {df['date_time'].max().date()}")
print(f"  Most sold item ......... {df['Item'].value_counts().idxmax()} ({df['Item'].value_counts().max():,})")
print(f"  Busiest hour ........... {hourly.idxmax()}:00 ({hourly.max():,} sales)")
print(f"  Busiest day of week .... {df['day_of_week'].value_counts().idxmax()}")
print(f"  Charts exported to ..... {os.path.abspath(EXPORT_DIR)}")
print("=" * 60)
