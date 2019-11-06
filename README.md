**TcpWireProtocol** представляет собой библиотеку, реализующую протокол прикладного уровня для удобной передачи данных.
Протокол построен на архитектуре «клиент-сервер» и использует одно сетевое соединение для отправки следующих сообщений:

1. TcpWireCommand - **запрос** (клиент => сервер)
1. TcpWireAnswer - **ответ на запрос** (сервер => клиент)
1. TcpWireEvent - **сообщение по инициативе сервера** (сервер => клиент)

------------

## **TcpWireCommand**
Внутренняя схема пакета:

| cmd id  | service  | command  | length  |  payload |
| ------------ | ------------ | ------------ | ------------ | ------------ |
|  int | short  |  short |  int | byte[]  |
| 4 bytes  | 2 bytes  | 2 bytes  |  4 bytes |  *length* bytes |

| #  | Описание  |
| ------------ | ------------ |
| cmd id  | Внутренний порядковый номер запроса  |
|  service | Служба, для которой предназначен этот запрос (например **Auth**)  |
|  command |  Команда внутри службы, определяющая обработчик запроса (например **Registration**, **Login**, **Logout** ) |
| length  | Длина полезной нагрузки  |
| payload  | Полезная нагрузка  |

Пример использования:
```csharp
TcpWireCommand cmd = new TcpWireCommand(service, command, payload);
client.SendToServer(cmd.RawBuffer);
```

## **TcpWireAnswer**
Внутренняя схема пакета:

| cmd id |  length  |  payload |
| ------------|------------ | ------------ |
|  int |  int | byte[]  |
| 4 bytes   |  4 bytes |  *length* bytes |

| #  | Описание  |
| ------------ | ------------ |
| cmd id  | Порядковый номер запроса, на который необходимо ответить  |
| length  | Длина полезной нагрузки  |
| payload  | Полезная нагрузка  |

Пример использования:
```csharp
TcpWireAnswer ans = new TcpWireAnswer(cmd.cmdId, payload);
server.SendToClient(ans.RawBuffer);
```
или
```csharp
TcpWireAnswer answer = cmd.CreateAnswer(payload);
server.SendToClient(ans.RawBuffer);
```

## **TcpWireEvent**
Внутренняя схема пакета:

| cmd id  | length  |  service | command | payload |
| ------------ | ------------ | ------------ | ------------ | ------------ |
|  int | int  |  short |  short | byte[]  |
| 4 bytes  | 4 bytes  | 2 bytes  |  4 bytes |  *length* bytes |


| #  | Описание  |
| ------------ | ------------ |
| cmd id  | Всегда равен нулю |
|  service | Служба, для которой предназначен это сообщение (например **Auth**)  |
|  command |  Команда внутри службы, определяющая обработчик запроса (например **New user**, **User online**, **User offline** ) |
| length  | `sizeof(service)` + `sizeof(command)` + Длина полезной нагрузки |
| payload  | Полезная нагрузка  |

Пример использования:
```csharp
TcpWireEvent evt = new TcpWireEvent(service, command, payload);
server.SendToClient(evt.RawBuffer);
```
