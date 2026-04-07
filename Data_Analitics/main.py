import base64
import io
import os
import sys
import warnings
from html import escape

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import seaborn as sns
from plotly.subplots import make_subplots

warnings.filterwarnings("ignore")
sns.set_theme(style="whitegrid", palette="muted", font_scale=1.1)

# Paths
CSV_PATH = os.path.join(
    os.path.dirname(__file__), "..", "Persistence", "Docs", "bakery_sales_revised.csv"
)
EXPORT_DIR = os.path.join(
    os.path.dirname(__file__), "..", "BusinessInteligence", "wwwroot", "charts"
)
os.makedirs(EXPORT_DIR, exist_ok=True)


# Step collector ; each step stores code, text output, and chart HTML

STEPS: list[dict] = []


class _CaptureOutput:
    """Context manager to capture print() output into a string."""
    def __enter__(self):
        self._buf = io.StringIO()
        self._old = sys.stdout
        sys.stdout = self._buf
        return self

    def __exit__(self, *_):
        sys.stdout = self._old

    @property
    def text(self) -> str:
        return self._buf.getvalue()


def _mpl_to_b64(fig) -> str:
    """Render a matplotlib figure to a base-64 PNG data-URI and close it."""
    buf = io.BytesIO()
    fig.savefig(buf, format="png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    buf.seek(0)
    return "data:image/png;base64," + base64.b64encode(buf.read()).decode()


def _plotly_to_html(fig) -> str:
    """Return the inner HTML <div> for an interactive Plotly chart."""
    return fig.to_html(full_html=False, include_plotlyjs=False)


def add_step(title: str, code: str, output: str,
             chart_img: str | None = None,
             plotly_html: str | None = None):
    STEPS.append({
        "title": title,
        "code": code,
        "output": output,
        "chart_img": chart_img,
        "plotly_html": plotly_html,
    })


# Also keep the old save-to-disk behaviour
def save_file(fig, name: str, *, plotly_fig=False):
    path = os.path.join(EXPORT_DIR, name)
    if plotly_fig:
        fig.write_html(path.replace(".png", ".html"))
    else:
        fig.savefig(path, dpi=150, bbox_inches="tight")



# STEP 1 ; Load Data

CODE_1 = '''\
import pandas as pd

CSV_PATH = "Persistence/Docs/bakery_sales_revised.csv"
df = pd.read_csv(CSV_PATH)
print("Shape:", df.shape)
print()
print(df.head())'''

df = pd.read_csv(CSV_PATH)

with _CaptureOutput() as cap:
    print("Shape:", df.shape)
    print()
    print(df.head())

add_step("1. Load Data", CODE_1, cap.text)


# STEP 2 ; Basic Exploration

CODE_2 = '''\
print("DATA TYPES:")
print(df.dtypes)
print("\\nBASIC STATISTICS:")
print(df.describe(include="all"))
print("\\nMISSING VALUES:")
print(df.isnull().sum())
print("\\nDUPLICATED ROWS:", df.duplicated().sum())
print("\\nUNIQUE ITEMS:", df["Item"].nunique())'''

with _CaptureOutput() as cap:
    print("DATA TYPES:")
    print(df.dtypes)
    print("\nBASIC STATISTICS:")
    print(df.describe(include="all"))
    print("\nMISSING VALUES:")
    print(df.isnull().sum())
    print("\nDUPLICATED ROWS:", df.duplicated().sum())
    print("\nUNIQUE ITEMS:", df["Item"].nunique())

add_step("2. Basic Exploration", CODE_2, cap.text)


# STEP 3 ; Data Preprocessing

CODE_3 = '''\
df["date_time"] = pd.to_datetime(df["date_time"], format="%m/%d/%Y %H:%M")
df["date"]       = df["date_time"].dt.date
df["month"]      = df["date_time"].dt.month
df["month_name"] = df["date_time"].dt.month_name()
df["day_of_week"]= df["date_time"].dt.day_name()
df["hour"]       = df["date_time"].dt.hour
df["year_month"] = df["date_time"].dt.to_period("M").astype(str)
print("DATE RANGE:", df["date_time"].min(), "->", df["date_time"].max())
print()
print(df.dtypes)'''

df["date_time"] = pd.to_datetime(df["date_time"], format="%m/%d/%Y %H:%M")
df["date"] = df["date_time"].dt.date
df["month"] = df["date_time"].dt.month
df["month_name"] = df["date_time"].dt.month_name()
df["day_of_week"] = df["date_time"].dt.day_name()
df["hour"] = df["date_time"].dt.hour
df["year_month"] = df["date_time"].dt.to_period("M").astype(str)

with _CaptureOutput() as cap:
    print("DATE RANGE:", df["date_time"].min(), "->", df["date_time"].max())
    print()
    print(df.dtypes)

add_step("3. Data Preprocessing", CODE_3, cap.text)


# STEP 4 ; Per-Column Analysis (EDA)

CODE_4 = '''\
def stats_column(data, col):
    if data[col].dtype in ("float64", "int64"):
        print(f"{col}  ->  Quantitative ({data[col].dtype})")
        q1, q3 = data[col].quantile(0.25), data[col].quantile(0.75)
        iqr = q3 - q1
        ll, ul = q1 - 1.5*iqr, q3 + 1.5*iqr
        print(f"  Mean={data[col].mean():.2f}  Median={data[col].median():.2f}")
        print(f"  Std={data[col].std():.2f}  Min={data[col].min()}  Max={data[col].max()}")
        print(f"  Q1={q1}  Q3={q3}  IQR={iqr}")
        print(f"  Lower Limit={ll}  Upper Limit={ul}")
        out = ((data[col]<ll)|(data[col]>ul)).sum()
        print(f"  Outliers: {out}")
        print(f"  Skewness: {data[col].skew():.4f}")
    else:
        print(f"{col}  ->  Qualitative ({data[col].dtype})")
        print(f"  Unique={data[col].nunique()}  Mode={data[col].mode().iloc[0]}")
        for v,c in data[col].value_counts().head(5).items():
            print(f"    {v}: {c}")
    print()

for c in df.columns:
    stats_column(df, c)'''


def stats_column(data, col):
    if data[col].dtype in ("float64", "int64"):
        print(f"{col}  ->  Quantitative ({data[col].dtype})")
        q1 = data[col].quantile(0.25)
        q3 = data[col].quantile(0.75)
        iqr = q3 - q1
        ll = q1 - 1.5 * iqr
        ul = q3 + 1.5 * iqr
        print(f"  Mean ............. {data[col].mean():.4f}")
        print(f"  Median ........... {data[col].median():.4f}")
        print(f"  Std Dev .......... {data[col].std():.4f}")
        print(f"  Min .............. {data[col].min()}")
        print(f"  Max .............. {data[col].max()}")
        print(f"  Q1 (25%) ......... {q1}")
        print(f"  Q2 (50%) ......... {data[col].quantile(0.50)}")
        print(f"  Q3 (75%) ......... {q3}")
        print(f"  Q4 (100%) ........ {data[col].quantile(1.00)}")
        print(f"  IQR .............. {iqr}")
        print(f"  Lower Limit ...... {ll}")
        print(f"  Upper Limit ...... {ul}")
        outliers = ((data[col] < ll) | (data[col] > ul)).sum()
        print(f"  Outliers ......... {outliers}")
        print(f"  Skewness ......... {data[col].skew():.4f}")
    else:
        n_unique = data[col].nunique()
        cat_type = "Binary" if n_unique == 2 else ("Single value" if n_unique == 1 else "Multi-class")
        print(f"{col}  ->  Qualitative ({data[col].dtype})")
        print(f"  Unique values .... {n_unique}  ({cat_type})")
        mode_val = data[col].mode()
        print(f"  Mode ............. {mode_val.iloc[0] if not mode_val.empty else 'N/A'}")
        print(f"  Value Counts:")
        for val, cnt in data[col].value_counts().head(10).items():
            print(f"    {val}: {cnt}")
        if n_unique > 10:
            print(f"    ... and {n_unique - 10} more")
    print()


with _CaptureOutput() as cap:
    for col_name in df.columns:
        stats_column(df, col_name)

add_step("4. Per-Column Analysis (EDA)", CODE_4, cap.text)


# STEP 5 ; Dataset Summary

CODE_5 = '''\
print(f"Columns:  {list(df.columns)}")
print(f"Shape:    {df.shape}")
print(f"Size:     {df.size}")
print(f"Memory:   {df.memory_usage(deep=True).sum():,} bytes")
print(f"Missing:  {df.isnull().sum().sum()}")
print(f"Unique:   {df.nunique().sum()}")
print(f"Dupes:    {df.duplicated().sum()}")'''

with _CaptureOutput() as cap:
    print(f"Columns ................ {list(df.columns)}")
    print(f"Shape .................. {df.shape}")
    print(f"Size ................... {df.size}")
    print(f"Dimensions ............. {df.ndim}")
    print(f"Memory Usage (bytes) ... {df.memory_usage(deep=True).sum():,}")
    print(f"Total Missing Values ... {df.isnull().sum().sum()}")
    print(f"Total Unique Values .... {df.nunique().sum()}")
    print(f"Total Non-Null Values .. {df.count().sum()}")
    print(f"Total Duplicate Rows ... {df.duplicated().sum()}")

add_step("5. Dataset Summary", CODE_5, cap.text)


# STEP 6 ; Correlation Matrix & Skewness

CODE_6 = '''\
numeric_cols = df.select_dtypes(include=[np.number])
corr_matrix = numeric_cols.corr()
print(corr_matrix)
print("\\nSkewness:")
print(numeric_cols.skew())

sns.heatmap(corr_matrix, annot=True, fmt=".2f", cmap="coolwarm",
            vmin=-1, vmax=1, linewidths=0.5)'''

numeric_cols = df.select_dtypes(include=[np.number])
corr_matrix = numeric_cols.corr()

with _CaptureOutput() as cap:
    print(corr_matrix)
    print("\nSkewness:")
    print(numeric_cols.skew())

chart_img_6 = None
if numeric_cols.shape[1] >= 2:
    fig, ax = plt.subplots(figsize=(10, 8))
    sns.heatmap(corr_matrix, annot=True, fmt=".2f", cmap="coolwarm",
                vmin=-1, vmax=1, linewidths=0.5, ax=ax)
    ax.set_title("Correlation Matrix ; Numeric Columns", fontsize=16, fontweight="bold")
    save_file(fig, "00_correlation_matrix.png")
    chart_img_6 = _mpl_to_b64(fig)

add_step("6. Correlation Matrix & Skewness", CODE_6, cap.text, chart_img=chart_img_6)


# STEP 7 ; Top 15 Best-Selling Items

CODE_7 = '''\
top_items = df["Item"].value_counts().head(15)
print(top_items)

fig, ax = plt.subplots(figsize=(12, 6))
ax.barh(top_items.index[::-1], top_items.values[::-1])
ax.set_title("Top 15 Best-Selling Items")
ax.set_xlabel("Number of Sales")'''

top_items = df["Item"].value_counts().head(15)

with _CaptureOutput() as cap:
    print(top_items)

fig, ax = plt.subplots(figsize=(12, 6))
colors = sns.color_palette("viridis", len(top_items))
bars = ax.barh(top_items.index[::-1], top_items.values[::-1], color=colors)
ax.bar_label(bars, padding=3)
ax.set_title("Top 15 Best-Selling Items", fontsize=16, fontweight="bold")
ax.set_xlabel("Number of Sales")
save_file(fig, "01_top15_items.png")
img_7 = _mpl_to_b64(fig)

add_step("7. Top 15 Best-Selling Items", CODE_7, cap.text, chart_img=img_7)


# STEP 8 ; Pie Chart: Top 10 Items Share

CODE_8 = '''\
top10 = df["Item"].value_counts().head(10)
other = df["Item"].value_counts().iloc[10:].sum()
pie_data = pd.concat([top10, pd.Series({"Others": other})])

fig_pie = px.pie(names=pie_data.index, values=pie_data.values,
    title="Sales Distribution - Top 10 Items vs Others", hole=0.4)'''

top10 = df["Item"].value_counts().head(10)
other = df["Item"].value_counts().iloc[10:].sum()
pie_data = pd.concat([top10, pd.Series({"Others": other})])

with _CaptureOutput() as cap:
    print("Top 10 items + Others:")
    print(pie_data)

fig_pie = px.pie(
    names=pie_data.index, values=pie_data.values,
    title="Sales Distribution ; Top 10 Items vs Others",
    hole=0.4, color_discrete_sequence=px.colors.qualitative.Set3,
)
fig_pie.update_traces(textposition="inside", textinfo="percent+label")
save_file(fig_pie, "02_pie_top10.png", plotly_fig=True)
plotly_8 = _plotly_to_html(fig_pie)

add_step("8. Pie Chart ; Top 10 Items Share", CODE_8, cap.text, plotly_html=plotly_8)


# STEP 9 ; Sales by Period of Day

CODE_9 = '''\
period_order = ["morning", "afternoon", "evening", "night"]
period_counts = df["period_day"].value_counts().reindex(period_order).dropna()
print(period_counts)

ax.bar(period_counts.index, period_counts.values)
ax.set_title("Sales by Period of Day")'''

period_order = ["morning", "afternoon", "evening", "night"]
period_counts = df["period_day"].value_counts().reindex(period_order).dropna()

with _CaptureOutput() as cap:
    print(period_counts)

fig, ax = plt.subplots(figsize=(8, 5))
palette = {"morning": "#FFD700", "afternoon": "#FF8C00", "evening": "#4B0082", "night": "#191970"}
bars = ax.bar(period_counts.index, period_counts.values,
              color=[palette.get(p, "#888") for p in period_counts.index])
ax.bar_label(bars, padding=3, fontsize=11)
ax.set_title("Sales by Period of Day", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
save_file(fig, "03_period_of_day.png")
img_9 = _mpl_to_b64(fig)

add_step("9. Sales by Period of Day", CODE_9, cap.text, chart_img=img_9)


# STEP 10 ; Weekday vs Weekend

CODE_10 = '''\
ww_counts = df["weekday_weekend"].value_counts()
print(ww_counts)

ax.bar(ww_counts.index, ww_counts.values, color=["#3498db", "#e74c3c"])
ax.set_title("Sales: Weekday vs Weekend")'''

ww_counts = df["weekday_weekend"].value_counts()

with _CaptureOutput() as cap:
    print(ww_counts)

fig, ax = plt.subplots(figsize=(6, 5))
bars = ax.bar(ww_counts.index, ww_counts.values, color=["#3498db", "#e74c3c"], width=0.5)
ax.bar_label(bars, padding=3, fontsize=12)
ax.set_title("Sales: Weekday vs Weekend", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
save_file(fig, "04_weekday_weekend.png")
img_10 = _mpl_to_b64(fig)

add_step("10. Weekday vs Weekend", CODE_10, cap.text, chart_img=img_10)


# STEP 11 ; Heatmap: Items x Period of Day

CODE_11 = '''\
top_items_list = df["Item"].value_counts().head(12).index.tolist()
heat_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "period_day"]).size().unstack(fill_value=0)
)
sns.heatmap(heat_data, annot=True, fmt="d", cmap="YlOrRd")'''

top_items_list = df["Item"].value_counts().head(12).index.tolist()
heat_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "period_day"]).size().unstack(fill_value=0)
    .reindex(columns=[p for p in period_order if p in df["period_day"].unique()])
)

with _CaptureOutput() as cap:
    print(heat_data)

fig, ax = plt.subplots(figsize=(10, 7))
sns.heatmap(heat_data, annot=True, fmt="d", cmap="YlOrRd", linewidths=0.5, ax=ax)
ax.set_title("Top 12 Items x Period of Day", fontsize=16, fontweight="bold")
save_file(fig, "05_heatmap_item_period.png")
img_11 = _mpl_to_b64(fig)

add_step("11. Heatmap ; Items x Period of Day", CODE_11, cap.text, chart_img=img_11)


# STEP 12 ; Heatmap: Items x Day of Week

CODE_12 = '''\
day_order = ["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"]
heat_day = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item","day_of_week"]).size().unstack(fill_value=0)
    .reindex(columns=day_order)
)
sns.heatmap(heat_day, annot=True, fmt="d", cmap="coolwarm")'''

day_order = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
heat_day = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "day_of_week"]).size().unstack(fill_value=0)
    .reindex(columns=day_order)
)

with _CaptureOutput() as cap:
    print(heat_day)

fig, ax = plt.subplots(figsize=(12, 7))
sns.heatmap(heat_day, annot=True, fmt="d", cmap="coolwarm", linewidths=0.5, ax=ax)
ax.set_title("Top 12 Items x Day of Week", fontsize=16, fontweight="bold")
save_file(fig, "06_heatmap_item_dayofweek.png")
img_12 = _mpl_to_b64(fig)

add_step("12. Heatmap ; Items x Day of Week", CODE_12, cap.text, chart_img=img_12)


# STEP 13 ; Stacked Bar: Period x Weekday/Weekend

CODE_13 = '''\
stacked = (
    df.groupby(["period_day","weekday_weekend"]).size()
    .unstack(fill_value=0)
)
stacked.plot(kind="bar", stacked=True, color=["#3498db","#e74c3c"])'''

stacked = (
    df.groupby(["period_day", "weekday_weekend"]).size().unstack(fill_value=0)
    .reindex([p for p in period_order if p in df["period_day"].unique()])
)

with _CaptureOutput() as cap:
    print(stacked)

fig, ax = plt.subplots(figsize=(9, 6))
stacked.plot(kind="bar", stacked=True, color=["#3498db", "#e74c3c"], ax=ax)
ax.set_title("Period of Day ; Weekday vs Weekend", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Sales")
ax.set_xticklabels(ax.get_xticklabels(), rotation=0)
ax.legend(title="Day Type")
save_file(fig, "07_stacked_period_ww.png")
img_13 = _mpl_to_b64(fig)

add_step("13. Stacked Bar ; Period x Weekday/Weekend", CODE_13, cap.text, chart_img=img_13)


# STEP 14 ; Daily Transactions Over Time

CODE_14 = '''\
daily = df.groupby("date")["Transaction"].nunique().reset_index()
daily.columns = ["date", "transactions"]
daily["date"] = pd.to_datetime(daily["date"])

fig_ts = px.line(daily, x="date", y="transactions",
    title="Daily Unique Transactions Over Time")'''

daily = df.groupby("date")["Transaction"].nunique().reset_index()
daily.columns = ["date", "transactions"]
daily["date"] = pd.to_datetime(daily["date"])

with _CaptureOutput() as cap:
    print(f"Days in dataset: {len(daily)}")
    print(f"Avg transactions/day: {daily['transactions'].mean():.1f}")
    print(daily.head(10))

fig_ts = px.line(
    daily, x="date", y="transactions",
    title="Daily Unique Transactions Over Time",
    labels={"date": "Date", "transactions": "Unique Transactions"},
)
fig_ts.update_traces(line=dict(width=1.5))
fig_ts.update_layout(hovermode="x unified")
save_file(fig_ts, "08_daily_transactions.png", plotly_fig=True)
plotly_14 = _plotly_to_html(fig_ts)

add_step("14. Daily Transactions Over Time", CODE_14, cap.text, plotly_html=plotly_14)


# STEP 15 ; Monthly Sales Volume

CODE_15 = '''\
monthly = df.groupby("year_month").size().reset_index(name="sales")
print(monthly)

ax.fill_between(monthly["year_month"], monthly["sales"], alpha=0.3)
ax.plot(monthly["year_month"], monthly["sales"], marker="o")'''

monthly = df.groupby("year_month").size().reset_index(name="sales")

with _CaptureOutput() as cap:
    print(monthly.to_string(index=False))

fig, ax = plt.subplots(figsize=(12, 5))
ax.fill_between(monthly["year_month"], monthly["sales"], alpha=0.3, color="#2ecc71")
ax.plot(monthly["year_month"], monthly["sales"], marker="o", color="#27ae60")
ax.set_title("Monthly Sales Volume", fontsize=16, fontweight="bold")
ax.set_ylabel("Number of Items Sold")
ax.set_xlabel("Month")
plt.xticks(rotation=45, ha="right")
save_file(fig, "09_monthly_sales.png")
img_15 = _mpl_to_b64(fig)

add_step("15. Monthly Sales Volume", CODE_15, cap.text, chart_img=img_15)


# STEP 16 ; Hourly Sales Distribution

CODE_16 = '''\
hourly = df.groupby("hour").size()
print(hourly)

ax.bar(hourly.index, hourly.values)
ax.set_title("Sales Distribution by Hour")'''

hourly = df.groupby("hour").size()

with _CaptureOutput() as cap:
    print(hourly)

fig, ax = plt.subplots(figsize=(12, 5))
ax.bar(hourly.index, hourly.values, color=sns.color_palette("rocket", len(hourly)), edgecolor="white")
ax.set_title("Sales Distribution by Hour", fontsize=16, fontweight="bold")
ax.set_xlabel("Hour of Day")
ax.set_ylabel("Number of Sales")
ax.set_xticks(hourly.index)
save_file(fig, "10_hourly_distribution.png")
img_16 = _mpl_to_b64(fig)

add_step("16. Hourly Sales Distribution", CODE_16, cap.text, chart_img=img_16)


# STEP 17 ; Treemap: Items per Period

CODE_17 = '''\
tree_df = (
    df.groupby(["period_day","Item"]).size()
    .reset_index(name="count").sort_values("count", ascending=False)
)
fig_tree = px.treemap(tree_df, path=["period_day","Item"],
    values="count", color="count", color_continuous_scale="Viridis")'''

tree_df = (
    df.groupby(["period_day", "Item"]).size()
    .reset_index(name="count").sort_values("count", ascending=False)
)

with _CaptureOutput() as cap:
    print(tree_df.head(15).to_string(index=False))

fig_tree = px.treemap(
    tree_df, path=["period_day", "Item"], values="count",
    title="Treemap: Items Sold per Period of Day",
    color="count", color_continuous_scale="Viridis",
)
save_file(fig_tree, "11_treemap.png", plotly_fig=True)
plotly_17 = _plotly_to_html(fig_tree)

add_step("17. Treemap ; Items per Period", CODE_17, cap.text, plotly_html=plotly_17)


# STEP 18 ; Sunburst: Day Type -> Period -> Item

CODE_18 = '''\
sun_df = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["weekday_weekend","period_day","Item"]).size()
    .reset_index(name="count")
)
fig_sun = px.sunburst(sun_df,
    path=["weekday_weekend","period_day","Item"], values="count")'''

sun_df = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["weekday_weekend", "period_day", "Item"]).size()
    .reset_index(name="count")
)

with _CaptureOutput() as cap:
    print(f"Rows in sunburst data: {len(sun_df)}")
    print(sun_df.head(10).to_string(index=False))

fig_sun = px.sunburst(
    sun_df, path=["weekday_weekend", "period_day", "Item"], values="count",
    title="Sunburst: Day Type -> Period -> Item",
    color="count", color_continuous_scale="RdBu",
)
save_file(fig_sun, "12_sunburst.png", plotly_fig=True)
plotly_18 = _plotly_to_html(fig_sun)

add_step("18. Sunburst ; Day Type / Period / Item", CODE_18, cap.text, plotly_html=plotly_18)


# STEP 19 ; Bubble Chart: Hour x Day of Week

CODE_19 = '''\
bubble = df.groupby(["day_of_week","hour"]).size().reset_index(name="count")
fig_bubble = px.scatter(bubble, x="hour", y="day_of_week",
    size="count", color="count", size_max=30,
    color_continuous_scale="YlOrRd")'''

bubble = df.groupby(["day_of_week", "hour"]).size().reset_index(name="count")
bubble["day_of_week"] = pd.Categorical(bubble["day_of_week"], categories=day_order, ordered=True)

with _CaptureOutput() as cap:
    print(bubble.head(15).to_string(index=False))

fig_bubble = px.scatter(
    bubble, x="hour", y="day_of_week", size="count", color="count",
    title="Bubble Chart: Sales by Hour x Day of Week",
    labels={"hour": "Hour", "day_of_week": "Day of Week", "count": "Sales"},
    color_continuous_scale="YlOrRd", size_max=30,
)
fig_bubble.update_layout(yaxis=dict(categoryorder="array", categoryarray=day_order))
save_file(fig_bubble, "13_bubble_hour_day.png", plotly_fig=True)
plotly_19 = _plotly_to_html(fig_bubble)

add_step("19. Bubble Chart ; Hour x Day of Week", CODE_19, cap.text, plotly_html=plotly_19)


# STEP 20 ; Violin Plot: Hour per Period

CODE_20 = '''\
order = [p for p in period_order if p in df["period_day"].unique()]
sns.violinplot(data=df, x="period_day", y="hour", order=order, palette="Set2")'''

order = [p for p in period_order if p in df["period_day"].unique()]

with _CaptureOutput() as cap:
    for p in order:
        subset = df[df["period_day"] == p]["hour"]
        print(f"{p:12s}  mean={subset.mean():.1f}  median={subset.median():.0f}  "
              f"std={subset.std():.1f}  min={subset.min()}  max={subset.max()}")

fig, ax = plt.subplots(figsize=(10, 6))
sns.violinplot(data=df, x="period_day", y="hour", order=order, palette="Set2", ax=ax)
ax.set_title("Hour Distribution per Period of Day", fontsize=16, fontweight="bold")
save_file(fig, "14_violin_hour_period.png")
img_20 = _mpl_to_b64(fig)

add_step("20. Violin Plot ; Hour Distribution per Period", CODE_20, cap.text, chart_img=img_20)


# STEP 21 ; Monthly Trend: Top 5 Items

CODE_21 = '''\
top5 = df["Item"].value_counts().head(5).index.tolist()
top5_monthly = (
    df[df["Item"].isin(top5)]
    .groupby(["year_month","Item"]).size().reset_index(name="count")
)
fig_facet = px.line(top5_monthly, x="year_month", y="count",
    color="Item", facet_col="Item", facet_col_wrap=3)'''

top5 = df["Item"].value_counts().head(5).index.tolist()
top5_monthly = (
    df[df["Item"].isin(top5)]
    .groupby(["year_month", "Item"]).size().reset_index(name="count")
)

with _CaptureOutput() as cap:
    print("Top 5 items:", top5)
    print()
    print(top5_monthly.head(15).to_string(index=False))

fig_facet = px.line(
    top5_monthly, x="year_month", y="count", color="Item",
    facet_col="Item", facet_col_wrap=3,
    title="Monthly Trend ; Top 5 Items",
    labels={"year_month": "Month", "count": "Sales"},
)
fig_facet.update_xaxes(tickangle=45)
fig_facet.for_each_annotation(lambda a: a.update(text=a.text.split("=")[-1]))
save_file(fig_facet, "15_top5_monthly_facet.png", plotly_fig=True)
plotly_21 = _plotly_to_html(fig_facet)

add_step("21. Monthly Trend ; Top 5 Items", CODE_21, cap.text, plotly_html=plotly_21)


# STEP 22 ; Sales by Day of Week (grouped bar)

CODE_22 = '''\
dow_ww = df.groupby(["day_of_week","weekday_weekend"]).size().reset_index(name="count")
fig_dow = px.bar(dow_ww, x="day_of_week", y="count",
    color="weekday_weekend", barmode="group")'''

dow_ww = (
    df.groupby(["day_of_week", "weekday_weekend"]).size().reset_index(name="count")
)
dow_ww["day_of_week"] = pd.Categorical(dow_ww["day_of_week"], categories=day_order, ordered=True)
dow_ww.sort_values("day_of_week", inplace=True)

with _CaptureOutput() as cap:
    print(dow_ww.to_string(index=False))

fig_dow = px.bar(
    dow_ww, x="day_of_week", y="count", color="weekday_weekend",
    title="Sales by Day of Week",
    labels={"day_of_week": "Day", "count": "Sales", "weekday_weekend": "Type"},
    color_discrete_map={"weekday": "#3498db", "weekend": "#e74c3c"},
    barmode="group",
)
save_file(fig_dow, "16_day_of_week_bar.png", plotly_fig=True)
plotly_22 = _plotly_to_html(fig_dow)

add_step("22. Sales by Day of Week", CODE_22, cap.text, plotly_html=plotly_22)


# STEP 23 ; Heatmap: Top Items x Hour

CODE_23 = '''\
corr_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item","hour"]).size().unstack(fill_value=0)
)
sns.heatmap(corr_data, cmap="magma_r", annot=True, fmt="d")'''

corr_data = (
    df[df["Item"].isin(top_items_list)]
    .groupby(["Item", "hour"]).size().unstack(fill_value=0)
)

with _CaptureOutput() as cap:
    print(corr_data)

fig, ax = plt.subplots(figsize=(14, 8))
sns.heatmap(corr_data, cmap="magma_r", annot=True, fmt="d", linewidths=0.3, ax=ax)
ax.set_title("Top Items x Hour of Day", fontsize=16, fontweight="bold")
ax.set_xlabel("Hour")
save_file(fig, "17_heatmap_item_hour.png")
img_23 = _mpl_to_b64(fig)

add_step("23. Heatmap ; Top Items x Hour", CODE_23, cap.text, chart_img=img_23)


# STEP 24 ; Transaction Size Analysis

CODE_24 = '''\
txn_size = df.groupby("Transaction").agg(
    items_count=("Item","count"), hour=("hour","first"),
    period=("period_day","first"), day_type=("weekday_weekend","first"),
).reset_index()
print(txn_size.describe())

# Histogram + boxplots for period and day type'''

txn_size = df.groupby("Transaction").agg(
    items_count=("Item", "count"),
    hour=("hour", "first"),
    period=("period_day", "first"),
    day_type=("weekday_weekend", "first"),
).reset_index()

with _CaptureOutput() as cap:
    print("Transaction size statistics:")
    print(txn_size["items_count"].describe())
    print(f"\nMost common basket size: {txn_size['items_count'].mode().iloc[0]}")
    print(f"Max items in one transaction: {txn_size['items_count'].max()}")

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
save_file(fig, "18_transaction_size.png")
img_24 = _mpl_to_b64(fig)

add_step("24. Transaction Size Analysis", CODE_24, cap.text, chart_img=img_24)


# STEP 25 ; Final Summary

CODE_25 = '''\
print(f"Total rows:        {len(df):,}")
print(f"Unique transactions: {df['Transaction'].nunique():,}")
print(f"Unique items:      {df['Item'].nunique()}")
print(f"Date range:        {df['date_time'].min().date()} -> {df['date_time'].max().date()}")
print(f"Most sold item:    {df['Item'].value_counts().idxmax()}")
print(f"Busiest hour:      {hourly.idxmax()}:00")
print(f"Busiest day:       {df['day_of_week'].value_counts().idxmax()}")'''

with _CaptureOutput() as cap:
    print(f"Total rows ............. {len(df):,}")
    print(f"Unique transactions .... {df['Transaction'].nunique():,}")
    print(f"Unique items ........... {df['Item'].nunique()}")
    print(f"Date range ............. {df['date_time'].min().date()} -> {df['date_time'].max().date()}")
    print(f"Most sold item ......... {df['Item'].value_counts().idxmax()} ({df['Item'].value_counts().max():,})")
    print(f"Busiest hour ........... {hourly.idxmax()}:00 ({hourly.max():,} sales)")
    print(f"Busiest day of week .... {df['day_of_week'].value_counts().idxmax()}")

add_step("25. Final Summary", CODE_25, cap.text)



# BUILD HTML REPORT


def build_html(steps: list[dict]) -> str:
    total = len(steps)

    # Build each step's HTML
    step_cards = []
    for i, s in enumerate(steps):
        code_html = escape(s["code"])
        output_html = escape(s["output"]) if s["output"].strip() else ""

        chart_block = ""
        if s.get("plotly_html"):
            chart_block = f'<div class="chart-plotly">{s["plotly_html"]}</div>'
        elif s.get("chart_img"):
            chart_block = f'<div class="chart-img"><img src="{s["chart_img"]}" alt="chart"></div>'

        output_block = ""
        if output_html:
            output_block = f'''
            <div class="output-label">Output</div>
            <pre class="output">{output_html}</pre>'''

        step_cards.append(f'''
    <div class="step" id="step-{i}" style="display:none">
      <div class="step-header">
        <span class="step-badge">Step {i+1} / {total}</span>
        <h2>{escape(s["title"])}</h2>
      </div>
      <div class="code-label">Code</div>
      <pre class="code"><code>{code_html}</code></pre>
      {output_block}
      {chart_block}
    </div>''')

    steps_html = "\n".join(step_cards)

    return f'''<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Bakery Sales ; Data Analytics Notebook</title>
<script src="https://cdn.plot.ly/plotly-2.27.0.min.js"></script>
<style>
  :root {{
    --bg:        #f7f7f8;
    --card-bg:   #ffffff;
    --border:    #e0e0e0;
    --code-bg:   #1e1e2e;
    --code-fg:   #cdd6f4;
    --out-bg:    #f0f4f8;
    --accent:    #4361ee;
    --accent2:   #3a0ca3;
    --text:      #1a1a2e;
    --muted:     #6c757d;
    --badge-bg:  #4361ee;
    --badge-fg:  #fff;
  }}
  *, *::before, *::after {{ box-sizing: border-box; }}
  body {{
    margin: 0; font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    background: var(--bg); color: var(--text);
  }}

  .nav {{
    position: sticky; top: 0; z-index: 100;
    background: var(--card-bg); border-bottom: 1px solid var(--border);
    display: flex; align-items: center; justify-content: center;
    gap: 12px; padding: 10px 20px;
    box-shadow: 0 2px 6px rgba(0,0,0,.06);
  }}
  .nav button {{
    padding: 8px 22px; border: 1px solid var(--accent); border-radius: 6px;
    background: var(--accent); color: #fff; font-size: .92rem;
    cursor: pointer; transition: .2s;
  }}
  .nav button:hover {{ background: var(--accent2); border-color: var(--accent2); }}
  .nav button:disabled {{ opacity:.4; cursor:default; }}
  .nav .counter {{
    font-size: .95rem; font-weight: 600; min-width: 90px; text-align: center;
  }}

  .progress-bar {{
    height: 4px; background: var(--border);
  }}
  .progress-bar .fill {{
    height: 100%; background: var(--accent); transition: width .3s;
  }}

  .step-dots {{
    display: flex; justify-content: center; gap: 6px;
    padding: 10px 20px; flex-wrap: wrap;
    background: var(--card-bg); border-bottom: 1px solid var(--border);
  }}
  .step-dots .dot {{
    width: 28px; height: 28px; border-radius: 50%;
    border: 2px solid var(--border); background: var(--card-bg);
    font-size: .72rem; font-weight: 700; color: var(--muted);
    display: flex; align-items: center; justify-content: center;
    cursor: pointer; transition: .2s;
  }}
  .step-dots .dot.active {{
    border-color: var(--accent); background: var(--accent); color: #fff;
  }}
  .step-dots .dot:hover {{ border-color: var(--accent); }}

  .container {{ max-width: 1100px; margin: 0 auto; padding: 24px 20px 60px; }}

  .step {{ animation: fadeIn .3s ease; }}
  @keyframes fadeIn {{ from {{ opacity:0; transform:translateY(8px); }} to {{ opacity:1; transform:none; }} }}

  .step-header {{ margin-bottom: 12px; }}
  .step-badge {{
    display: inline-block; padding: 3px 10px; border-radius: 12px;
    background: var(--badge-bg); color: var(--badge-fg);
    font-size: .78rem; font-weight: 600; margin-bottom: 6px;
  }}
  .step-header h2 {{ margin: 4px 0 0; font-size: 1.35rem; }}

  .code-label, .output-label {{
    font-size: .8rem; font-weight: 600; text-transform: uppercase;
    letter-spacing: .5px; color: var(--muted); margin: 14px 0 4px;
  }}
  pre.code {{
    background: var(--code-bg); color: var(--code-fg);
    padding: 16px 20px; border-radius: 8px; overflow-x: auto;
    font-size: .88rem; line-height: 1.55; margin: 0;
  }}
  pre.output {{
    background: var(--out-bg); padding: 14px 18px; border-radius: 8px;
    overflow-x: auto; font-size: .84rem; line-height: 1.5;
    border: 1px solid var(--border); margin: 0; white-space: pre-wrap;
    max-height: 450px; overflow-y: auto;
  }}
  .chart-img {{ margin-top: 16px; text-align: center; }}
  .chart-img img {{ max-width: 100%; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.1); }}
  .chart-plotly {{ margin-top: 16px; }}

  .kbd-hint {{
    text-align: center; padding: 8px; font-size: .8rem; color: var(--muted);
  }}
  kbd {{
    background: var(--out-bg); border: 1px solid var(--border);
    border-radius: 4px; padding: 2px 6px; font-size: .78rem;
  }}
</style>
</head>
<body>
<div class="nav">
  <button id="btn-prev" onclick="go(-1)">&#9664; Anterior</button>
  <span class="counter" id="counter">1 / {total}</span>
  <button id="btn-next" onclick="go(1)">Próximo &#9654;</button>
</div>

<div class="progress-bar"><div class="fill" id="progress" style="width:{100/total:.2f}%"></div></div>

<div class="step-dots" id="dots"></div>

<div class="kbd-hint">Use <kbd>&#8592;</kbd> <kbd>&#8594;</kbd> teclas de seta para navegar</div>

<div class="container" id="container">
{steps_html}
</div>

<script>
const total = {total};
let cur = 0;

function render() {{
  document.querySelectorAll('.step').forEach((el,i) => el.style.display = i===cur ? 'block' : 'none');
  document.getElementById('counter').textContent = (cur+1) + ' / ' + total;
  document.getElementById('btn-prev').disabled = cur === 0;
  document.getElementById('btn-next').disabled = cur === total-1;
  document.getElementById('progress').style.width = ((cur+1)/total*100).toFixed(1)+'%';
  document.querySelectorAll('.dot').forEach((d,i) => d.classList.toggle('active', i===cur));
  const vis = document.getElementById('step-'+cur);
  if (vis) {{
    vis.querySelectorAll('.chart-plotly .plotly-graph-div').forEach(g => Plotly.Plots.resize(g));
  }}
}}

function go(delta) {{
  const next = cur + delta;
  if (next >= 0 && next < total) {{ cur = next; render(); window.scrollTo({{top:0,behavior:'smooth'}}); }}
}}

function jumpTo(i) {{
  cur = i; render(); window.scrollTo({{top:0,behavior:'smooth'}});
}}

const dotsEl = document.getElementById('dots');
for (let i = 0; i < total; i++) {{
  const d = document.createElement('span');
  d.className = 'dot' + (i===0?' active':'');
  d.textContent = i+1;
  d.onclick = () => jumpTo(i);
  dotsEl.appendChild(d);
}}

document.addEventListener('keydown', e => {{
  if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {{ e.preventDefault(); go(1); }}
  if (e.key === 'ArrowLeft'  || e.key === 'ArrowUp')   {{ e.preventDefault(); go(-1); }}
}});

render();
</script>
</body>
</html>'''


REPORT_PATH = os.path.join(EXPORT_DIR, "notebook_report.html")
html = build_html(STEPS)
with open(REPORT_PATH, "w", encoding="utf-8") as f:
    f.write(html)

print(f"\n{'='*60}")
print(f"HTML notebook report exported to:")
print(f"  {os.path.abspath(REPORT_PATH)}")
print(f"  Total steps: {len(STEPS)}")
print(f"{'='*60}")
