# Gozon Shop - Микросервисная система интернет-магазина

Микросервисная система для обработки заказов и асинхронных платежей в интернет-магазине.

### Алгоритм обработки платежа

- Order Service создает заказ и outbox-сообщение в одной транзакции.
- OutboxPublisher (Orders) отправляет PaymentRequest в RabbitMQ.
- PaymentRequestConsumer получает сообщение, проверяет дубликаты через Inbox.
- PaymentOrchestrator обрабатывает платеж, а именно:
  Проверяет существование счета;
  Проверяет достаточность средств;
  Списание денег или отказ;
  Создает outbox-сообщение с результатом.
- OutboxPublisher (Payments) отправляет PaymentResult в RabbitMQ.
- PaymentResultConsumer получает результат, обновляет статус заказа.
- Идемпотентность точно есть в обоих сервисах, через Inbox (проверка EventId).

### Запуск, остановка, проверка статуса

**Сборка и запуск:**
- docker compose build
- docker compose up -d

**Посмотреть статус контейнера:**
- docker compose ps

**Остановка:**
- docker compose down
- docker compose down -v (с удалением данных)

### Адреса сервисов

- Frontend - http://localhost:3000
- API Gateway - http://localhost:8080
- Orders Service - http://localhost:5001/health
- Payments Service - http://localhost:5002/health
- RabbitMQ Management UI - http://localhost:15672 (логин: `guest`, пароль: `guest`)


**Проверка здоровья:**
- curl http://localhost:8080/health
- curl http://localhost:5001/health
- curl http://localhost:5002/health


### Мониторинг

**Проверка RabbitMQ:**

Проверка очередей:
docker compose exec rabbitmq rabbitmqctl list_queues name messages

Проверка статуса RabbitMQ:
docker compose exec rabbitmq rabbitmqctl status


**Проверка баз данных:**

Orders база данных:
docker compose exec orders-db psql -U postgres -d orders -c "SELECT id, status, amount FROM orders ORDER BY created_at DESC LIMIT 5;"

Payments база данных:
docker compose exec payments-db psql -U postgres -d payments -c "SELECT user_id, balance FROM accounts LIMIT 5;"


### Логи:

Просмотр логов всех сервисов:
- docker compose logs -f

У конкретного:
- docker compose logs orders-service -f
- docker compose logs payments-service -f
- docker compose logs api-gateway
- docker compose logs frontend
- docker compose logs orders-db
- docker compose logs payments-db
- docker compose logs rabbitmq


### Пример рабочего процесса

1. Запуск системы:
docker compose up -d

2. Создание пользователя и пополнение счета:
USER_ID=$(uuidgen)
curl -X POST "http://localhost:8080/accounts" -H "user_id: $USER_ID" -d '{}'
curl -X POST "http://localhost:8080/accounts/$USER_ID/topup" -d '{"amount": 3000}'


3. Создание нескольких заказов:
curl -X POST "http://localhost:8080/orders" \
  -H "user_id: $USER_ID" \
  -d '{"amount": 20270, "description": "Пусть будут духи"}'

curl -X POST "http://localhost:8080/orders" \
  -H "user_id: $USER_ID" \
  -d '{"amount": 1500, "description": "Пусть будут роллы"}'


4. Проверка результатов:
Проверка списка заказов:
curl -X GET "http://localhost:8080/orders" -H "user_id: $USER_ID"

Проверка баланса:
curl -X GET "http://localhost:8080/accounts/$USER_ID/balance"

Мониторинг очередей RabbitMQ:
docker compose exec rabbitmq rabbitmqctl list_queues name messages


5. Остановка системы:
docker compose down

**Дополнительно:**

Из интересного можно такое попробовать что-то такое:

- Положить на счет 3000
- Купить на 500
- Купить на 800
- Купить на 2000
- Купить на 1500
- Купить на 10000
- Купить на 200
- Купить на 3200
- Купить на 802020
- Положить на счет 5000
- Купить на 4000
- Купить на 100
- Купить на 32920


### Frontend

После открытия http://localhost:3000 вы увидите:

- Ввод User ID - введите ID пользователя (его можно сгенерировать через терминал, например)
- Счет - создание счета, пополнение, просмотр баланса
- Создание заказа - форма для указания суммы и описания заказа
- Список заказов - таблица со всеми заказами пользователя
- Прямые API-вызовы через Frontend

Сначала входите как User, потом создаете счет, потом что хотите делаете.

Frontend делает запросы через API Gateway. Вы можете вызывать те же API напрямую.


### Postman коллекция

Полная коллекция для тестирования всех сценариев доступна в файле gozon-postman-collection.json.

Запуск через newman:
newman run gozon-postman-collection.json \
--env-var "baseUrl=http://localhost:8080"

## Основные API-эндпоинты

### Создание и пополнение счета

curl -X POST "http://localhost:8080/accounts" \
-H "Content-Type: application/json" \
-H "user_id: 550e8400-e29b-41d4-a716-446655440000" \
-d '{}'

curl -X POST "http://localhost:8080/accounts/550e8400-e29b-41d4-a716-446655440000/topup" \
-H "Content-Type: application/json" \
-d '{"amount": 5000}'

curl -X GET "http://localhost:8080/accounts/550e8400-e29b-41d4-a716-446655440000/balance"

### Работа с заказами

Создание заказа:
curl -X POST "http://localhost:8080/orders" \
-H "Content-Type: application/json" \
-H "user_id: 550e8400-e29b-41d4-a716-446655440000" \
-d '{"amount": 10389, "description": "Духи"}'

Получение списка заказов пользователя:
curl -X GET "http://localhost:8080/orders" \
-H "user_id: 550e8400-e29b-41d4-a716-446655440000"

Получение информации о конкретном заказе:
curl -X GET "http://localhost:8080/orders/{order-id}"
