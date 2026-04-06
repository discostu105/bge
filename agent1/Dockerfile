FROM python:3.12-slim

WORKDIR /app

# Install dependencies
COPY agent1/requirements.txt ./agent1/requirements.txt
RUN pip install --no-cache-dir -r agent1/requirements.txt

# Copy agent1 module and default config
COPY agent1/ ./agent1/
COPY config/ ./config/

# Run as non-root
RUN useradd -m -r -u 1001 botuser && chown -R botuser:botuser /app
USER botuser

# Env-var config: BGE_API_KEY, BGE_GAME_ID, BGE_DIFFICULTY, BGE_STRATEGY, BGE_BASE_URL
ENV BGE_BASE_URL=https://ageofagents.net \
    BGE_DIFFICULTY=medium \
    BGE_STRATEGY=balanced \
    PYTHONUNBUFFERED=1

CMD ["python", "-m", "agent1"]
