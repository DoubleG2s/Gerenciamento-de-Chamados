# 🎫 Sistema de Chamados (Tickets)

Sistema completo de gerenciamento de chamados técnicos desenvolvido em ASP.NET Core 8.0 com Razor Pages e PostgreSQL.

## 📋 Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Funcionalidades](#funcionalidades)
- [Tecnologias](#tecnologias)
- [Requisitos](#requisitos)
- [Instalação](#instalação)
- [Configuração](#configuração)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Deploy](#deploy)
- [Segurança](#segurança)
- [Contribuindo](#contribuindo)
- [Licença](#licença)

---

## 🚀 Sobre o Projeto

Sistema web para gerenciamento de chamados técnicos com funcionalidades de criação, atribuição, acompanhamento e resolução de tickets. Desenvolvido para atender empresas que necessitam organizar e priorizar solicitações de suporte técnico.

### 🎯 Objetivos

- ✅ Centralizar solicitações de suporte
- ✅ Priorizar chamados por urgência
- ✅ Acompanhar status em tempo real
- ✅ Anexar arquivos e documentação
- ✅ Gerenciar usuários e permissões
- ✅ Categorizar chamados por tipo

---

## ⚡ Funcionalidades

### 👤 Gestão de Usuários
- ✅ Três níveis de acesso: Admin, Técnico, Usuário
- ✅ Autenticação com Identity
- ✅ Alteração de senha
- ✅ Cadastro de usuários (Admin/Técnico)
- ✅ Gerenciamento de departamentos e cargos

### 🎫 Gestão de Chamados
- ✅ Criação de chamados com anexos (PDF, DOC, Excel, PowerPoint, imagens, ZIP, RAR)
- ✅ Categorização por tipo
- ✅ Priorização (Baixa, Média, Alta, Crítica)
- ✅ Status (Aberto, Em Andamento, Aguardando, Resolvido, Fechado)
- ✅ Comentários e histórico
- ✅ Anexos múltiplos (até 10MB cada, máx. 10 arquivos)

### 📊 Dashboards
- ✅ Visão geral de chamados por status
- ✅ Chamados por prioridade
- ✅ Chamados por categoria
- ✅ Relatórios e métricas

### ⚙️ Configurações
- ✅ Perfil do usuário
- ✅ Alteração de senha
- ✅ Temas (em desenvolvimento)
- ✅ Notificações (em desenvolvimento)

---

## 🛠️ Tecnologias

### Backend
- **ASP.NET Core 8.0** - Framework web
- **Razor Pages** - Engine de views
- **Entity Framework Core 8.0** - ORM
- **PostgreSQL** - Banco de dados
- **ASP.NET Core Identity** - Autenticação e autorização

### Frontend
- **Bootstrap 5.3** - Framework CSS
- **Bootstrap Icons** - Ícones
- **JavaScript Vanilla** - Interatividade
- **Chart.js** (planejado) - Gráficos e dashboards

### Infraestrutura
- **AWS RDS PostgreSQL** - Banco de dados em nuvem
- **AWS S3** (planejado) - Armazenamento de arquivos
- **AWS Elastic Beanstalk** (planejado) - Deploy da aplicação

---

## 📦 Requisitos

- **.NET SDK 8.0** ou superior
- **PostgreSQL 14+** (local) ou acesso ao AWS RDS
- **Visual Studio 2022** ou **Visual Studio Code**
- **Git**

### Extensões recomendadas (VS Code)
- C# Dev Kit
- C# Extensions
- NuGet Package Manager

---

## 🔧 Instalação

### 1. Clone o repositório

