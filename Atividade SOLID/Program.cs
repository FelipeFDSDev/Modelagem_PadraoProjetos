using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BibliotecaSólida
{
    // =============================================================
    // 1. SRP - ENTIDADES E DTOs
    // =============================================================
    public class Livro
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public bool Disponivel { get; set; } = true;
        public DateTime? DataEmprestimo { get; set; }
        public string EmailUsuario { get; set; } = string.Empty;
        public string NomeUsuario { get; set; } = string.Empty;
    }

    // Objeto de valor para representar a transação de empréstimo (Conforme seu diagrama)
    public class Emprestimo
    {
        public DateTime Data { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
    }

    // =============================================================
    // 2. OCP - ESTRATÉGIAS DE DESCONTO (Strategy Pattern)
    // =============================================================
    public interface IDescontoStrategy { decimal CalcularDesconto(decimal valor); }
    public class DescontoEstudante : IDescontoStrategy { public decimal CalcularDesconto(decimal v) => v * 0.50m; }
    public class DescontoProfessor : IDescontoStrategy { public decimal CalcularDesconto(decimal v) => v * 0.80m; }
    public class DescontoFuncionario : IDescontoStrategy { public decimal CalcularDesconto(decimal v) => v * 0.30m; }
    public class SemDesconto : IDescontoStrategy { public decimal CalcularDesconto(decimal v) => 0; }

    // =============================================================
    // 3. SRP - LÓGICA DE NEGÓCIO ISOLADA
    // =============================================================
    public class CalculadoraMulta
    {
        public decimal Calcular(Livro livro)
        {
            if (livro.DataEmprestimo == null) return 0;
            var diasAtraso = (DateTime.Now - livro.DataEmprestimo.Value).Days - 14;
            return diasAtraso <= 0 ? 0 : diasAtraso * 2.50m;
        }
    }

    // =============================================================
    // 4. ISP - INTERFACES SEGREGADAS (Relatórios e Infra)
    // =============================================================
    public interface IRelatorioPDF { void GerarPDF(string conteudo); }
    public interface IRelatorioExcel { void GerarExcel(string conteudo); }
    public interface IRelatorioHTML { void GerarHTML(string conteudo); } // MANTIDO TUDO
    public interface IEnvioNotificacao { void Enviar(string para, string msg); }
    public interface ISalvarDisco { void Salvar(string caminho, string conteudo); }
    
    public interface IBancoDados { 
        void Salvar(string tabela, string dados); 
        List<string> Buscar(string tabela, string filtro);
    }

    // =============================================================
    // 5. LSP - HIERARQUIA DE ACERVO CORRETA
    // =============================================================
    public interface IReservavel { void Reservar(string usuario); }

    public abstract class ItemAcervo
    {
        public string Titulo { get; set; } = string.Empty;
        public bool Disponivel { get; set; } = true;
        public abstract void Emprestar(string usuario);
        public abstract void Devolver();
    }

    public class LivroFisico : ItemAcervo, IReservavel
    {
        public override void Emprestar(string u) => Console.WriteLine($"[FÍSICO] '{Titulo}' emprestado para {u}.");
        public override void Devolver() => Console.WriteLine($"[FÍSICO] '{Titulo}' devolvido à estante.");
        public void Reservar(string u) => Console.WriteLine($"[FÍSICO] '{Titulo}' reservado para {u}.");
    }

    public class Ebook : ItemAcervo
    {
        public override void Emprestar(string u) => Console.WriteLine($"[EBOOK] Link enviado para {u}.");
        public override void Devolver() => Console.WriteLine($"[EBOOK] Acesso digital revogado.");
    }

    // =============================================================
    // 6. DIP - SERVIÇOS DE ORQUESTRAÇÃO
    // =============================================================
    public class ServicoEmprestimo
    {
        private readonly IBancoDados _banco;
        private readonly IEnvioNotificacao _notificador;
        private readonly CalculadoraMulta _calculadora;

        public ServicoEmprestimo(IBancoDados banco, IEnvioNotificacao notificador, CalculadoraMulta calc)
        {
            _banco = banco;
            _notificador = notificador;
            _calculadora = calc;
        }

        public void RealizarEmprestimo(Livro livro, string nome, string email)
        {
            if (!livro.Disponivel) return;
            livro.Disponivel = false;
            livro.DataEmprestimo = DateTime.Now;
            _banco.Salvar("livros", $"Id: {livro.Id}, Titulo: {livro.Titulo}, Usuario: {nome}");
            Console.WriteLine($"Sucesso: {livro.Titulo} emprestado para {nome}");
        }

        public void DevolverComMulta(Livro livro, IDescontoStrategy strategy)
        {
            decimal multaBase = _calculadora.Calcular(livro);
            decimal desconto = strategy.CalcularDesconto(multaBase);
            decimal valorFinal = multaBase - desconto;

            livro.Disponivel = true;
            _banco.Salvar("livros", $"Devolução: {livro.Titulo}");

            if (valorFinal > 0)
                _notificador.Enviar(livro.EmailUsuario, $"Multa de R${valorFinal} gerada.");
        }
    }

    // =============================================================
    // IMPLEMENTAÇÕES CONCRETAS (INFRA)
    // =============================================================
    public class BancoMySQL : IBancoDados {
        public void Salvar(string t, string d) => Console.WriteLine($"[DB-MySQL] INSERT INTO {t} VALUES ({d})");
        public List<string> Buscar(string t, string f) => new List<string> { "Resultado MySQL" };
    }

    public class NotificadorEmail : IEnvioNotificacao {
        public void Enviar(string p, string m) => Console.WriteLine($"[SMTP] Para: {p} | Msg: {m}");
    }

    // Implementação de Relatório que usa apenas o que precisa
    public class RelatorioCompleto : IRelatorioPDF, IRelatorioExcel, IRelatorioHTML, ISalvarDisco {
        public void GerarPDF(string c) => Console.WriteLine($"[PDF] {c}");
        public void GerarExcel(string c) => Console.WriteLine($"[EXCEL] {c}");
        public void GerarHTML(string c) => Console.WriteLine($"[HTML] {c}");
        public void Salvar(string caminho, string conteudo) => Console.WriteLine($"[DISCO] Salvo em {caminho}");
    }

    // =============================================================
    // PROGRAMA PRINCIPAL
    // =============================================================
    class Program
    {
        static void Main()
        {
            // Injeção de Dependências Manual
            var banco = new BancoMySQL();
            var email = new NotificadorEmail();
            var calc = new CalculadoraMulta();
            var servico = new ServicoEmprestimo(banco, email, calc);

            var meuLivro = new Livro { Id = 1, Titulo = "Clean Architecture", EmailUsuario = "ads@fatec.edu" };

            // 1. Teste de Empréstimo
            servico.RealizarEmprestimo(meuLivro, "João Silva", "ads@fatec.edu");

            // 2. Teste de Multa (Simulando 20 dias de atraso)
            meuLivro.DataEmprestimo = DateTime.Now.AddDays(-20);
            servico.DevolverComMulta(meuLivro, new DescontoEstudante());

            // 3. Teste de Relatórios (ISP em ação)
            var relatorio = new RelatorioCompleto();
            relatorio.GerarHTML("Relatório de Inventário");
            relatorio.GerarExcel("Dados Financeiros");
            relatorio.Salvar("C:\\Relatorios\\log.txt", "Fim da Operação");
            
            Console.WriteLine("\n=== Sistema Finalizado com SOLID Total ===");
        }
    }
}