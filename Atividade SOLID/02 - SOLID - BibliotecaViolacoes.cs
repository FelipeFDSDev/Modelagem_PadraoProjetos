// using System;
// using System.Collections.Generic;

// namespace BibliotecaSólida
// {
//     // =============================================================
//     // 1. SRP - A classe Livro agora é uma Entidade Pura (POCO).
//     // Ela não sabe mais como se salvar, enviar e-mail ou calcular multa.
//     // =============================================================
//     public class Livro
//     {
//         public int Id { get; set; }
//         public string Titulo { get; set; }
//         public string Autor { get; set; }
//         public bool Disponivel { get; set; } = true;
//         public DateTime? DataEmprestimo { get; set; }
//         public string EmailUsuario { get; set; }
//         public string NomeUsuario { get; set; }
//     }

//     // =============================================================
//     // 2. DIP - Interfaces para abstrair a infraestrutura.
//     // O sistema não depende mais de MySQL ou SMTP diretamente.
//     // =============================================================
//     public interface IRepository { void Salvar(string tabela, string dados); }
//     public interface IEmailService { void Enviar(string para, string assunto, string corpo); }

//     // =============================================================
//     // 3. OCP - Padrão Strategy para Descontos.
//     // Para criar "Idoso" ou "VIP", basta criar uma nova classe sem mudar o serviço.
//     // =============================================================
//     public interface IDescontoStrategy { decimal Calcular(decimal valorMulta); }

//     public class DescontoEstudante : IDescontoStrategy {
//         public decimal Calcular(decimal valor) => valor * 0.50m;
//     }
//     public class DescontoProfessor : IDescontoStrategy {
//         public decimal Calcular(decimal valor) => valor * 0.80m;
//     }

//     // =============================================================
//     // 4. ISP & LSP - Segregação de Interfaces e Substituição de Liskov.
//     // Ebooks não são forçados a ter "Reserva Física".
//     // =============================================================
//     public interface IReservavel { void Reservar(string usuario); }

//     public abstract class ItemAcervo
//     {
//         public string Titulo { get; set; }
//         public bool Disponivel { get; set; }
//         public abstract void Emprestar(string usuario);
//     }

//     public class LivroFisico : ItemAcervo, IReservavel
//     {
//         public override void Emprestar(string u) => Console.WriteLine($"[FÍSICO] {Titulo} emprestado.");
//         public void Reservar(string u) => Console.WriteLine($"[FÍSICO] {Titulo} reservado.");
//     }

//     public class Ebook : ItemAcervo
//     {
//         public override void Emprestar(string u) => Console.WriteLine($"[EBOOK] Link enviado para {u}.");
//         // Não implementa IReservavel, então não lança NotImplementedException.
//     }

//     // =============================================================
//     // SERVIÇO REFATORADO - O "Cérebro" do Sistema
//     // =============================================================
//     public class ServicoEmprestimo
//     {
//         private readonly IRepository _repo;
//         private readonly IEmailService _email;

//         // Injeção de Dependência: o serviço recebe as ferramentas que precisa de fora.
//         public ServicoEmprestimo(IRepository repo, IEmailService email)
//         {
//             _repo = repo;
//             _email = email;
//         }

//         public void RealizarEmprestimo(Livro livro, string nome, string email)
//         {
//             if (!livro.Disponivel) return;

//             livro.Disponivel = false;
//             livro.DataEmprestimo = DateTime.Now;
//             _repo.Salvar("livros", livro.Titulo);
//             Console.WriteLine($"Sucesso: {livro.Titulo} para {nome}");
//         }

//         public void DevolverComMulta(Livro livro, IDescontoStrategy strategy)
//         {
//             // Lógica de multa extraída para o serviço
//             decimal multaBase = 20.0m; 
//             decimal valorFinal = strategy.Calcular(multaBase);

//             livro.Disponivel = true;
//             _repo.Salvar("livros", $"Devolução de {livro.Titulo}");

//             if (valorFinal > 0)
//                 _email.Enviar(livro.EmailUsuario, "Multa", $"Valor: R${valorFinal}");
//         }
//     }

//     // =============================================================
//     // PROGRAMA PRINCIPAL (Exemplo de uso)
//     // =============================================================
//     class Program
//     {
//         static void Main()
//         {
//             // Simulando um "Container de Injeção"
//             var repo = new MySqlRepository(); 
//             var email = new SmtpService();
//             var servico = new ServicoEmprestimo(repo, email);

//             var livro = new Livro { Titulo = "Design Patterns", EmailUsuario = "ads@fatec.edu" };

//             // Usando Polimorfismo e Strategy
//             servico.RealizarEmprestimo(livro, "João", "joao@email.com");
//             servico.DevolverComMulta(livro, new DescontoEstudante());
//         }
//     }

//     // Classes de Infra (Simulação)
//     public class MySqlRepository : IRepository { 
//         public void Salvar(string t, string d) => Console.WriteLine($"[SQL] Salvo em {t}"); 
//     }
//     public class SmtpService : IEmailService { 
//         public void Enviar(string p, string a, string c) => Console.WriteLine($"[SMTP] Enviado para {p}"); 
//     }
// }