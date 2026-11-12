FROM python:3.14-slim
WORKDIR /app
COPY server.py ./server.py
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
EXPOSE 8767
# The hosting platform supplies PORT and provider secrets at runtime.
CMD ["python", "-B", "server.py", "--host", "0.0.0.0"]
