# Sistema de Pedidos — Demo de Modernizacao .NET

Aplicacao completa em **ASP.NET Core 9 (Razor Pages)** que demonstra a modernizacao incremental de um sistema monolitico sincrono para uma **arquitetura orientada a mensagens** com resiliencia.

> **Cenario do Desafio:** Sistema legado monolitico em .NET Framework com alto acoplamento e chamadas sincronas.
> CreateInvoice() - grava DB - chama API externa - envia email — tudo sincrono, tudo acoplado.
>
> **Objetivo:** Reestruturar utilizando mensageria e arquitetura resiliente.

---

## Arquitetura Utilizada

### Antes vs Depois

    ANTES (Monolito Sincrono):
    CreateInvoice() - Grava BD - Chama API - Envia Email
    (tudo sincrono, se a API cair = erro para o usuario)

    DEPOIS (Arquitetura Moderna):
    CreateOrder() - Grava BD - Publica Mensagem (retorna imediatamente!)
    Worker 1 - Chama API (com retry automatico)
    Worker 2 - Envia Email (com retry automatico)
    (desacoplado, resiliente, o usuario nao espera)

### Fluxo Completo

    Usuario clica "Criar Pedido" no browser
      |
    Razor Page (Create.cshtml.cs) chama OrderService.CreateAsync()
      |
    OrderService salva no SQLite via AppDbContext
      |
    OrderService publica OrderMessage no CompositeMessageBus
      |
    CompositeMessageBus distribui para 2 canais (fan-out):
      |-- apiBus   - ExternalApiWorker consome - chama API (com retry Polly)
      |-- emailBus - EmailWorker consome - EmailService - Gmail SMTP (com retry Polly)
      |
    Usuario ja viu a resposta (nao esperou API nem email!)

### Padroes e Conceitos Aplicados

| Padrao | Descricao |
|--------|-----------|
| **Mensageria Assincrona** | System.Threading.Channels simula RabbitMQ/Azure Service Bus |
| **Pub/Sub (Fan-out)** | Uma mensagem publicada - multiplos consumidores independentes |
| **Background Workers** | BackgroundService consome mensagens fora do ciclo da requisicao HTTP |
| **Resiliencia (Polly)** | Retry com backoff exponencial em cada worker |
| **Desacoplamento** | O servico salva no banco e publica a mensagem. Workers fazem o resto |
| **Razor Pages** | Padrao MVVM simplificado para o CRUD (PageModel + View) |
| **EF Core + SQLite** | Zero configuracao de infraestrutura — banco criado automaticamente |
| **MailKit** | Envio de emails reais via SMTP |

---

## Estrutura do Projeto

    Teste/
    ├── Program.cs                          <- Composicao raiz (DI, middlewares, startup)
    ├── Teste.csproj                        <- Definicao do projeto e pacotes NuGet
    ├── appsettings.json                    <- Configuracoes (SMTP, logging)
    │
    ├── Data/
    │   └── AppDbContext.cs                 <- EF Core — ponte entre C# e SQLite
    │
    ├── Models/
    │   ├── Pedido.cs                       <- Modelo do pedido (tabela pedidos)
    │   └── ItemPedido.cs                   <- Modelo do item (tabela itens_pedido)
    │
    ├── Messaging/
    │   ├── OrderMessage.cs                 <- Contrato da mensagem (record imutavel)
    │   ├── MessageBus.cs                   <- Canal de mensagens em memoria (Channels)
    │   └── CompositeMessageBus.cs          <- Fan-out — distribui para multiplos canais
    │
    ├── Services/
    │   ├── OrderService.cs                 <- Logica de negocio (BD + publicar mensagem)
    │   └── EmailService.cs                 <- Envio real de email via SMTP (MailKit)
    │
    ├── Workers/
    │   ├── ExternalApiWorker.cs            <- Consome mensagens - chama API (Polly retry)
    │   └── EmailWorker.cs                  <- Consome mensagens - envia email (Polly retry)
    │
    └── Pages/
        ├── Index.cshtml / .cs              <- Home — comparacao Antes vs Depois
        ├── Shared/
        │   └── _Layout.cshtml              <- Layout principal com navegacao
        ├── Pedidos/
        │   ├── Index.cshtml / .cs          <- Lista pedidos
        │   ├── Create.cshtml / .cs         <- Criar pedido - dispara mensageria
        │   ├── Edit.cshtml / .cs           <- Editar pedido
        │   └── Delete.cshtml / .cs         <- Excluir pedido (cascata)
        └── Itens/
            ├── Index.cshtml / .cs          <- Lista itens de um pedido
            ├── Create.cshtml / .cs         <- Adicionar item - recalcula total
            ├── Edit.cshtml / .cs           <- Editar item - recalcula total
            └── Delete.cshtml / .cs         <- Excluir item - recalcula total

---

## Detalhamento de Cada Arquivo

### Teste.csproj — Arquivo de Projeto

Define o projeto .NET, a versao do framework e os pacotes NuGet.

| Pacote | Funcao |
|---|---|
| Microsoft.EntityFrameworkCore.Sqlite | Acesso ao banco SQLite via EF Core |
| Polly.Core | Resiliencia — retry automatico com backoff exponencial |
| MailKit | Envio de emails reais via SMTP |

### Program.cs — Composicao Raiz (Entry Point)

E o **ponto de entrada** da aplicacao. Configura e conecta TODAS as pecas:

1. Registra o banco de dados (SQLite)
2. Cria os canais de mensageria (apiBus + emailBus)
3. Monta o CompositeMessageBus (fan-out)
4. Registra os servicos (OrderService, EmailService)
5. Registra os Workers em background
6. Cria o banco automaticamente (EnsureCreatedAsync)
7. Configura o pipeline HTTP (middlewares)
8. Inicia a aplicacao

**Comunicacao:** E quem "amarra" todas as dependencias via Injecao de Dependencia (DI).

### Data/AppDbContext.cs — Contexto do Banco de Dados

E a ponte entre o C# e o banco SQLite. Define:
- As tabelas (Pedidos, ItensPedido)
- Os relacionamentos (1 Pedido - N Itens)
- A regra de exclusao em cascata (deletar pedido = deletar itens)

**Comunicacao:** Usado pelo OrderService e pelas paginas de Itens para ler/gravar dados.

### Models/Pedido.cs — Modelo de Pedido

Representa um **pedido** no sistema. Mapeia para a tabela pedidos.

| Propriedade | Significado |
|---|---|
| OrderId | Chave primaria (auto-increment) |
| CustomerName | Nome do cliente |
| CustomerEmail | Email do cliente (usado para enviar confirmacao) |
| OrderDate | Data de criacao |
| Status | Pendente, Confirmado, Enviado, Cancelado |
| TotalAmount | Soma dos itens (calculado automaticamente) |
| Itens | Lista de itens do pedido (navegacao) |

**Comunicacao:** Usado em TODAS as camadas — Models - DbContext - Service - Razor Pages.

### Models/ItemPedido.cs — Modelo de Item do Pedido

Representa uma **linha de produto** dentro de um pedido. Mapeia para itens_pedido.

| Propriedade | Significado |
|---|---|
| ItemId | Chave primaria |
| OrderId | FK para o pedido pai |
| ProductId | ID do produto (simulado) |
| Quantity | Quantidade |
| UnitPrice | Preco unitario |
| LineTotal | Calculado: Quantity x UnitPrice (nao salvo no banco) |

**Comunicacao:** Ligado ao Pedido via FK OrderId.

### Messaging/OrderMessage.cs — Contrato da Mensagem

Define o **formato da mensagem** que trafega pelo barramento. E um record (imutavel).

    OrderMessage(OrderId, CustomerName, CustomerEmail, Action)

**Comunicacao:** Publicado pelo OrderService, consumido pelo ExternalApiWorker e EmailWorker.

### Messaging/MessageBus.cs — Canal de Mensagens

E o **barramento de mensagens em memoria** usando System.Threading.Channels. Funciona como uma fila:
- **Produtor** chama PublishAsync() - coloca a mensagem na fila
- **Consumidor** chama ReadAllAsync() - le as mensagens conforme chegam

**Em producao** seria substituido por RabbitMQ, Azure Service Bus ou AWS SQS — sem mudar a logica de negocio.

    OrderService - PublishAsync() - [Canal] - ReadAllAsync() - Worker

### Messaging/CompositeMessageBus.cs — Fan-out (Distribuidor)

Implementa o padrao **Pub/Sub**. Quando uma mensagem e publicada, ela e enviada para **TODOS** os canais assinantes simultaneamente.

    CompositeMessageBus
      ├── MessageBus (API)   - ExternalApiWorker consome
      └── MessageBus (Email) - EmailWorker consome

**Comunicacao:** OrderService publica uma vez - dois workers recebem independentemente.

### Services/OrderService.cs — Logica de Negocio

Contem toda a **logica de negocio** dos pedidos. E o coracao da modernizacao:

| Metodo | O que faz |
|---|---|
| GetAllAsync() | Lista todos os pedidos com itens |
| GetByIdAsync(id) | Busca um pedido por ID |
| CreateAsync(pedido) | **Salva no BD + publica mensagem** <- modernizado! |
| UpdateAsync(pedido) | Atualiza dados do pedido |
| DeleteAsync(id) | Remove pedido (cascata deleta itens) |
| RecalculateTotalAsync(id) | Recalcula total quando itens mudam |

**Comunicacao:** Recebe AppDbContext (banco) e CompositeMessageBus (mensageria) via DI.

### Services/EmailService.cs — Envio Real de Email

Envia emails **reais** via SMTP usando MailKit. Le as credenciais do appsettings.json.

**Fluxo:**
1. Monta a mensagem (MimeMessage) com HTML
2. Conecta ao servidor SMTP (Gmail) com TLS
3. Autentica com usuario/senha
4. Envia o email
5. Desconecta

**Comunicacao:** Chamado pelo EmailWorker quando uma mensagem chega no barramento.

### Workers/ExternalApiWorker.cs — Worker de API Externa

Roda em **background** (nao bloqueia o usuario). Simula a chamada a uma API externa (ex: gateway de pagamento, ERP). Usa **Polly** para retry:

    Tentativa 1 - falhou - espera 2s
    Tentativa 2 - falhou - espera 4s
    Tentativa 3 - falhou - espera 8s (backoff exponencial)

**Comunicacao:** Consome mensagens do apiBus (seu proprio canal).

### Workers/EmailWorker.cs — Worker de Email

Roda em **background**. Quando recebe uma mensagem do barramento:
1. Cria um escopo de DI (IServiceScopeFactory)
2. Obtem o EmailService
3. Monta o HTML do email
4. Envia via EmailService.SendAsync()
5. Se falhar, **Polly faz retry** automatico (3 tentativas)

**Comunicacao:** Consome mensagens do emailBus - chama EmailService - SMTP.

### Pages/ — Interface do Usuario (Razor Pages)

| Pagina | Funcao |
|---|---|
| Pages/Index.cshtml | Home — mostra a comparacao Antes vs Depois |
| Pages/Shared/_Layout.cshtml | Layout principal — menu de navegacao |
| Pages/Pedidos/Index | Lista todos os pedidos |
| Pages/Pedidos/Create | Formulario de criacao - **dispara a mensageria** |
| Pages/Pedidos/Edit | Edita dados do pedido |
| Pages/Pedidos/Delete | Confirma e deleta pedido (cascata) |
| Pages/Itens/Index | Lista itens de um pedido |
| Pages/Itens/Create | Adiciona item - recalcula total |
| Pages/Itens/Edit | Edita item - recalcula total |
| Pages/Itens/Delete | Remove item - recalcula total |

**Comunicacao:** Cada PageModel (*.cshtml.cs) injeta OrderService ou AppDbContext para acessar dados.

### appsettings.json — Configuracoes

Armazena configuracoes da aplicacao (log, SMTP, etc.) sem recompilar codigo.

---

## Perguntas Arquiteturais

### 1) Quando NAO utilizar microsservicos?

Microsservicos **nao sao a resposta padrao**. Na maioria dos cenarios, sao prematuros.

**Nao use microsservicos quando:**

| Cenario | Por que |
|---|---|
| **Equipe pequena** (1-5 devs) | A complexidade operacional supera o beneficio. Quem vai manter 10 deploys, 10 logs, 10 monitoramentos? |
| **Dominio simples ou desconhecido** | Se voce ainda nao entende as fronteiras do negocio, vai errar os limites dos servicos — e refatorar microsservicos e **muito mais caro** que refatorar um monolito |
| **Startup / MVP** | Velocidade importa mais que escalabilidade. Um monolito bem estruturado entrega mais rapido |
| **Sem infra de DevOps** | Microsservicos exigem CI/CD, containers, orquestracao, observabilidade. Sem isso - caos |
| **Dados altamente acoplados** | Se todos os servicos precisam das mesmas tabelas, voce so criou um **monolito distribuido** (pior dos dois mundos) |

**Este projeto e o exemplo perfeito**

O projeto e um **monolito modular** — e isso e **correto** para o cenario:

    Monolito Modular (este projeto)
      ├── Pastas separadas: Models / Services / Messaging / Workers
      ├── Um unico deploy, um unico banco
      └── Mensageria em memoria (pronto para extrair depois)

> **Regra de ouro:** Comece com um monolito bem organizado. Extraia microsservicos **apenas quando a dor justificar** (ex: uma parte precisa escalar 100x enquanto outra nao).

**Quando SIM usar microsservicos:**
- Equipes grandes (>20 devs) que precisam deploy independente
- Partes do sistema com requisitos de escala muito diferentes
- Dominio bem conhecido com fronteiras claras
- Infraestrutura de DevOps madura

---

### 2) Quais os trade-offs do CQRS?

**CQRS** (Command Query Responsibility Segregation) = separar o modelo de **leitura** do modelo de **escrita**.

**Este projeto ja tem um "CQRS leve" implicito:**

As queries usam AsNoTracking() (otimizado para leitura) enquanto os commands fazem tracking + SaveChanges:

    // QUERY — leitura otimizada, sem tracking
    public async Task<List<Pedido>> GetAllAsync()
        => await _db.Pedidos.Include(p => p.Itens)
            .AsNoTracking().ToListAsync();

    // COMMAND — escrita com tracking + side effect (mensagem)
    public async Task<Pedido> CreateAsync(Pedido pedido)
    {
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();
        await _bus.PublishAsync(...);
    }

**Trade-offs do CQRS completo:**

| Beneficio | Custo |
|---|---|
| Leitura e escrita otimizadas independentemente | **Complexidade** — dois modelos para manter |
| Escala leitura separada da escrita (ex: read replicas) | **Consistencia eventual** — leitura pode estar desatualizada |
| Modelos de leitura desnormalizados = queries rapidas | **Sincronizacao** — precisa de eventos para manter os lados em sincronia |
| Facilita auditoria e event sourcing | **Debugging dificil** — fluxo nao e linear |
| Cada lado evolui independentemente | **Overhead operacional** — mais bancos, mais infra |

**Quando CQRS faz sentido:**

    Sim: Leituras >>> Escritas (ex: e-commerce — 1000 visualizacoes por 1 compra)
    Sim: Modelos de leitura muito diferentes dos de escrita
    Sim: Necessidade de event sourcing / auditoria completa

    Nao: CRUD simples (nosso caso atual — CQRS seria over-engineering)
    Nao: Dominio simples com poucas queries
    Nao: Equipe sem experiencia com consistencia eventual

**O caminho progressivo (o que fizemos):**

    1. CRUD basico
       |
    2. Separar Read/Write no Service (AsNoTracking)  
       |
    3. Mensageria assincrona  <-- este projeto esta aqui
       |
    4. CQRS completo (bancos separados)

> **Este projeto esta no estagio 3** — mensageria assincrona sem CQRS completo. E o equilibrio ideal para a maioria dos projetos.

---

### 3) Como evitar over-engineering?

Over-engineering e **resolver problemas que voce nao tem**. E o erro mais comum em arquitetura de software.

**Sinais de over-engineering:**

| Sinal | Exemplo |
|---|---|
| Abstracoes desnecessarias | Criar IOrderRepository + OrderRepository + IOrderService + OrderService para um CRUD simples |
| Padroes "por garantia" | Implementar CQRS + Event Sourcing + Saga para um app de 3 tabelas |
| Infra desproporcional | Kubernetes + RabbitMQ + Redis + Elasticsearch para 100 usuarios |
| "E se no futuro..." | Projetar para 10 milhoes de usuarios quando tem 50 |
| Camadas demais | Controller - Service - Repository - UoW - DbContext (5 camadas para fazer _db.Save()) |

**O que fizemos de certo neste projeto:**

| Decisao | Por que e simples e correto |
|---|---|
| **SQLite** em vez de SQL Server/Postgres | Zero configuracao. Quando precisar, muda 1 linha no Program.cs |
| **Channel em memoria** em vez de RabbitMQ | Mesmo padrao pub/sub, zero infra. Quando precisar, troca o MessageBus |
| **Sem Repository Pattern** | AppDbContext ja E o repository. EF Core ja abstrai o banco |
| **Sem camada de DTO** | Os Models servem direto nas Razor Pages (projeto pequeno) |
| **OrderService faz tudo** | Uma classe clara. Nao precisa de 5 interfaces para 6 metodos |

**Regras praticas para evitar over-engineering:**

**1. YAGNI — "You Ain't Gonna Need It"**

    Nao implemente algo que voce TALVEZ precise no futuro.
    Implemente quando PRECISAR.

**2. Regra dos 3**

    Na primeira vez - implemente da forma mais simples.
    Na segunda vez - copie se necessario (sim, copie).
    Na terceira vez - agora sim, abstraia.

**3. Pergunte-se: "Qual problema concreto isso resolve HOJE?"**

    Errado: "Vou criar uma abstracao porque e best practice"
    Certo:  "Preciso trocar o banco? Nao? Entao nao crio IRepository"

**4. Modernizacao incremental (o que fizemos)**

    Monolito simples (CRUD + EF Core)
      | Dor: API sincrona trava o usuario
    Adicionar mensageria em memoria
      | Dor: email falha e perde
    Adicionar retry com Polly               <-- este projeto
      | Dor: 1 milhao de msgs/dia
    Trocar para RabbitMQ
      | Dor: equipe grande, deploy conflita
    Extrair microsservico

> **A melhor arquitetura e a mais simples que resolve o problema de HOJE, mas que esta organizada para evoluir AMANHA.**

Este projeto demonstra exatamente isso:
- **Simples**: SQLite, Channels em memoria, uma camada de servico
- **Pronto para evoluir**: trocar SQLite-Postgres = 1 linha; trocar Channel-RabbitMQ = 1 classe; extrair Worker-microsservico = mover a pasta

---

## Como Executar

**Pre-requisitos:** .NET 9 SDK - https://dotnet.microsoft.com/download/dotnet/9.0

    # Clone o repositorio
    git clone <url-do-repositorio>
    cd Teste

    # Restaure os pacotes
    dotnet restore

    # Execute a aplicacao
    dotnet run

O banco SQLite (pedidos.db) e **criado automaticamente** na primeira execucao.

### Configurar Email Real (opcional)

1. Acesse https://myaccount.google.com/apppasswords
2. Gere uma **Senha de App** (formato: abcd efgh ijkl mnop)
3. Edite o appsettings.json com suas credenciais

> NUNCA suba credenciais reais para o GitHub. Use dotnet user-secrets ou variaveis de ambiente em producao.

### Visualizar o Banco de Dados

Baixe o DB Browser for SQLite (https://sqlitebrowser.org/) e abra o arquivo pedidos.db gerado na raiz do projeto.

---

## Tecnologias

| Tecnologia | Versao | Uso |
    |---|---|---|
    | .NET | 9.0 | Runtime e SDK |
    | ASP.NET Core Razor Pages | 9.0 | Interface web (CRUD) |
    | Entity Framework Core | 9.0 | ORM — acesso ao banco de dados |
    | SQLite | — | Banco de dados (arquivo local) |
    | Polly | 8.5.2 | Resiliencia (retry com backoff exponencial) |
    | MailKit | 4.9.0 | Envio de emails via SMTP |
    | System.Threading.Channels | — | Mensageria em memoria (built-in .NET) |

---

## Licenca

Este projeto e uma demonstracao educacional de modernizacao de arquitetura .NET.
