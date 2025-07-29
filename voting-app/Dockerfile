FROM python:3.10-alpine AS builder

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

FROM python:3.10-alpine

WORKDIR /app

COPY --from=builder /usr/local/lib/python3.10/site-packages /usr/local/lib/python3.10/site-packages
COPY . .

EXPOSE 80

CMD ["python", "app.py"]
