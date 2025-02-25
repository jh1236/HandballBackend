FROM python:3.12-slim

ENV PYTHONUNBUFFERED=True

ENV APP_HOME=/app

ENV PORT=5001

WORKDIR $APP_HOME

COPY . .

RUN pip install --no-cache-dir -r requirements.txt

# Specify the command to run on container start
CMD [ "python", "./start.py", "-p", "${PORT}"]