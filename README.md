<h1 align="center">🎯 Sistema de Gerenciamento de Chamados</h1>

<p align="center">
  <img src="https://img.shields.io/badge/.NET%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/Blazor-5C2D91?style=for-the-badge&logo=blazor&logoColor=white"/>
  <img src="https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white"/>
  <img src="https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black"/>
  <img src="https://img.shields.io/badge/Entity%20Framework%20Core-512BD4?style=for-the-badge&logo=.net&logoColor=white"/>
  <img src="https://img.shields.io/badge/Status-Acad%C3%AAmico-blue?style=for-the-badge"/>
</p>

<p align="center">
  <b>Um sistema completo para gestão de chamados com relatórios, gráficos e assistente virtual com IA.</b>  
</p>

---

## 🧩 Sobre o Projeto

O **Gerenciamento de Chamados** é uma aplicação web desenvolvida em **ASP.NET Core com Blazor Pages**, voltada à **gestão de tickets** (chamados internos) de forma simples e visual.  
O sistema conta com **cartões dinâmicos para cada chamado**, **gráficos e relatórios analíticos**, e um **assistente virtual com Inteligência Artificial** 🤖.

Este projeto foi criado com **fins acadêmicos** nas dependências da  
🎓 **UNIP – Universidade Paulista (Ribeirão Preto)**, como parte de um trabalho de conclusão prático.

---

## 🚀 Funcionalidades

✅ **Gestão completa de chamados**
- Criar, editar, visualizar e excluir chamados.  
- Interface intuitiva com **cartões dinâmicos** e cores por status.  

📊 **Relatórios e gráficos**
- Estatísticas de produtividade, volume e status dos chamados.  
- Visualização em tempo real com gráficos interativos.  

🧠 **Assistente Virtual com IA**
- Auxilia usuários e fornece respostas automatizadas.  
- Desenvolvido com suporte de múltiplos modelos de IA:
  - ChatGPT 5  
  - Gemini Pro  
  - Claude Sonnet 4.5  
  - Perplexity Pro  
  - Grok  

🗄️ **Banco de Dados PostgreSQL**
- Armazenamento seguro e estruturado dos dados.  

🌐 **Interface Blazor**
- Front-end moderno e responsivo com **Blazor Pages**.  
- Recursos adicionais em **JavaScript** para interações leves.  

---

## 🏗️ Estrutura do Projeto



O projeto segue uma **estrutura modular organizada**:

📂 Gerenciamento-de-Chamados/ <br>
├── 📁 Models/ # Modelos de dados (Entidades, DTOs) <br>
├── 📁 Services/ # Lógica de negócio e integração com o banco de dados <br>
├── 📁 Pages/ # Páginas Blazor (UI) <br>
├── 📁 wwwroot/ # Recursos estáticos (CSS, JS, imagens) <br>
├── 📁 Data/ # Configuração e contexto do PostgreSQL <br>
├── 📄 appsettings.json # Configurações da aplicação <br>
└── 📄 Program.cs # Ponto de entrada da aplicação <br>


A aplicação foi desenvolvida com uma **arquitetura em camadas**, separando claramente:
- **Modelos (Models)** → definem a estrutura dos dados.  
- **Serviços (Services)** → contêm a lógica de negócio e acesso a dados.  
- **Páginas (Pages)** → representam a camada de apresentação (Blazor).  

---

## 🧰 Tecnologias Utilizadas

| Categoria | Tecnologia |
|------------|-------------|
| Linguagem | C# |
| Framework | ASP.NET Core |
| Front-end | Blazor Pages + JavaScript |
| Banco de Dados | PostgreSQL |
| ORM | Entity Framework Core |
| Ferramentas de IA | ChatGPT 5, Gemini Pro, Claude Sonnet 4.5, Perplexity Pro, Grok |
| IDE | Visual Studio / Visual Studio Code |
| Hospedagem | AWS para Banco de Dados |

---

## ⚙️ Como Executar o Projeto

1. **Clone este repositório:**
   ```bash
   git clone https://github.com/DoubleG2s/Gerenciamento-de-Chamados.git
   
2. Acesse a pasta do projeto:
cd Gerenciamento-de-Chamados


3. Configure o banco de dados PostgreSQL:
Crie um banco de dados no PostgreSQL.
Atualize a connectionString no arquivo appsettings.Development.json.

4. Execute as migrações (opcional):
dotnet ef database update


5. Inicie a aplicação:
dotnet run


6. Acesse no navegador:
http://localhost:5000

💡 Objetivo Acadêmico <br>

Este projeto foi desenvolvido exclusivamente para fins educacionais, como parte das atividades práticas do curso da UNIP – Universidade Paulista (Ribeirão Preto).
Ele representa a aplicação dos conhecimentos de desenvolvimento web, banco de dados, arquitetura de software e inteligência artificial aplicada.
<br>
🧑‍💻 Autoria e Créditos<br>
👨‍🎓 Desenvolvido por:<br>
Alunos Unip - Turma de ADS<br>
Projeto acadêmico desenvolvido com o auxílio de ferramentas de IA e orientação acadêmica.


🧠 Ferramentas de apoio à pesquisa e desenvolvimento:<br>
ChatGPT 5<br>
Gemini Pro<br>
Claude Sonnet 4.5<br>
Perplexity Pro<br>
Grok<br>
<br>
📜 Licença<br>
📄 Este projeto é de uso acadêmico e não comercial.
A redistribuição ou modificação é permitida apenas para fins educacionais e com devida menção ao autor original.
<br>
<p align="center"> ⭐ <b>Se este projeto te ajudou, não esqueça de deixar uma estrela no repositório!</b> ⭐ </p> ```
