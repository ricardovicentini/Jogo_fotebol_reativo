using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
//teste
namespace RxJogoFutebol2
{
    public interface IEvento { }
    public class ApitoInicio : IEvento { }
    public class ApitoFinal : IEvento { }
    public class ApitoGol : IEvento 
    {
        public ApitoGol(Time timeEmCampo)
        {
            TimeEmCampo = timeEmCampo;
        }

        public Time TimeEmCampo { get; set; }
    }

    public class ApitoFalta : IEvento
    {
        public ApitoFalta(Time timeEmCampo)
        {
            TimeEmCampo = timeEmCampo;
        }

        public Time TimeEmCampo { get; set; }
        
    }
    public class GolMarcado : IEvento
    {
        public GolMarcado(Time timeEmCampo, int golsMarcados)
        {
            TimeEmCampo = timeEmCampo;
            GolsMarcados = golsMarcados;
        }

        public Time TimeEmCampo { get; set; }
        public int GolsMarcados { get; set; }
    }

    public class FaltaCometida : IEvento
    {
        public FaltaCometida(Time timeEmCampo, int faltas)
        {
            TimeEmCampo = timeEmCampo;
            Faltas = faltas;
        }

        public Time TimeEmCampo { get; set; }
        public int Faltas { get; set; }
    }

    public class Derrota : IEvento 
    {
        public Derrota(Time timeEmCampo)
        {
            TimeEmCampo = timeEmCampo;
        }

        public Time TimeEmCampo { get; set; }
    }
    public class Vitoria : IEvento 
    {
        public Vitoria(Time timeEmCampo)
        {
            TimeEmCampo = timeEmCampo;
        }

        public Time TimeEmCampo { get; set; }
    }

    public class Empate : IEvento { }


    public class Regras
    {
        public Regras(int limiteFaltas, int limieteGols, int limiteTempo)
        {
            LimiteFaltas = limiteFaltas;
            LimieteGols = limieteGols;
            LimiteTempo = limiteTempo;
        }

        public int LimiteFaltas { get; set; }
        public int LimieteGols { get; set; }
        public int LimiteTempo { get; set; }
    }

    public class Ator
    {
        protected CentralEventos central;

        public Ator(CentralEventos central)
        {
            this.central = central ?? throw new ArgumentNullException(paramName: nameof(central));
        }
    }

    public class Time : Ator
    {
        public string Nome { get; private set; }
        private int GolsMarcados = 0;
        private int FaltasCometidas = 0;
        public Time(CentralEventos central, string nome) : base(central)
        {
            Nome = nome;

            central.OfType<ApitoGol>() // Gol contra o time
                .Where(gol => !gol.TimeEmCampo.Equals(this))
                .Subscribe(
                    gol => Console.WriteLine($"{Nome}: Ânimo pessoal!!! Vamos marcar mais forte")
                );

            central.OfType<ApitoGol>() // Gol do Time
                .Where(gol => gol.TimeEmCampo.Equals(this))
                .Subscribe(
                    gol => Console.WriteLine($"{Nome}: Muito bem pessoal!!!")
                );

            central.OfType<ApitoFalta>() //Falta do outro time
                .Where(falta => !falta.TimeEmCampo.Equals(this))
                .Subscribe(
                    falta => Console.WriteLine($"{Nome}: pô seu juiz, cadê o cartão?")
                );

            central.OfType<ApitoFalta>() //Falta do time
                .Where(falta => falta.TimeEmCampo.Equals(this))
                .Subscribe(
                    falta => Console.WriteLine($"{Nome}: foi na bola seu juiz, que injusto!")
                );

            central.OfType<Derrota>() // derrota por faltas do time
                .Where(falta => falta.TimeEmCampo.Equals(this))
                .Subscribe(
                    falta => Console.WriteLine($"{Nome}: Esse nr de faltas bem que poderia ser maior :(")
                );

            central.OfType<Derrota>() // derrota por faltas do outro time
                .Where(falta => !falta.TimeEmCampo.Equals(this))
                .Subscribe(
                    falta => Console.WriteLine($"{Nome}: Jogar com violência da nisso!")
                );

            central.OfType<Vitoria>() // vitoria do time
                .Where(v => v.TimeEmCampo.Equals(this))
                .Subscribe(
                    vit => Console.WriteLine($"{Nome}: Viva o {Nome}!")
                );

            central.OfType<Vitoria>() // vitoria do outro time
                .Where(v => !v.TimeEmCampo.Equals(this))
                .Subscribe(
                    vit => Console.WriteLine($"{Nome}: Na próxima {vit.TimeEmCampo.Nome} vocês vão ver!")
                );

            central.OfType<ApitoFinal>()
                .Subscribe(
                    fim => central.Parar()
                );
        }

        public void MarcarGol()
        {
            GolsMarcados++;
            central.Notificar(new GolMarcado(this,GolsMarcados));
        }

        public void CometerFalta()
        {
            FaltasCometidas++;
            central.Notificar(new FaltaCometida(this,FaltasCometidas));
        }
    }
    public class Juiz : Ator
    {
        private Regras _regras;
        public Juiz(CentralEventos central,Regras regras) : base(central)
        {
            _regras = regras;

            central.OfType<GolMarcado>()
                .Timeout(TimeSpan.FromSeconds(regras.LimiteTempo))
                .Subscribe(
                    e   => ApitarGol(e.TimeEmCampo,e.GolsMarcados),
                    ex  => FinalizarPorTempo(ex)
                );

            central.OfType<FaltaCometida>()
                .Timeout(TimeSpan.FromSeconds(regras.LimiteTempo))
                .Subscribe(
                    e  => ApitarFalta(e.TimeEmCampo, e.Faltas),
                    ex => FinalizarPorTempo(ex)
                );

        }
        private void FinalizarPorTempo(Exception ex)
        {
            Console.WriteLine($"Juiz: Fim de jogo, o tempo acabou! {ex.Message}");
            FinalizarPartida();
        }

        public void IniciarPartida()
        {
            central.Notificar(new ApitoInicio());
        }

        private void ApitarFalta(Time time, int faltas)
        {
            Console.WriteLine($"Juiz: Infração cometida pelo time {time.Nome}");
            central.Notificar(new ApitoFalta(time));
            if (faltas == _regras.LimiteFaltas)
            {
                Console.WriteLine($"Juiz: Fim de partida o time {time.Nome} atingiu o nr. máximo de faltas");
                central.Notificar(new Derrota(time));
                FinalizarPartida();
            }
            
        }

        private void ApitarGol(Time time,int golsMarcados)
        {
            Console.WriteLine($"Juiz: Gol para o time {time.Nome}");
            central.Notificar(new ApitoGol(time));
            if (golsMarcados == _regras.LimieteGols)
            {
                Console.WriteLine($"Juiz: Fim de partida o time {time.Nome} atingiu o nr. máximo de gols");
                central.Notificar(new Vitoria(time));
                FinalizarPartida();
            }
        }

        private void FinalizarPartida()
        {
            central.Notificar(new ApitoFinal());
            central.Parar();
        }
    }

    public class CentralEventos : IObservable<IEvento>
    {
        private Subject<IEvento> assinatura = new Subject<IEvento>();
        public IDisposable Subscribe(IObserver<IEvento> observer)
        {
            return assinatura.Subscribe(observer);
        }

        public void Notificar(IEvento evento)
        {
            assinatura.OnNext(evento);
        }

        public void Parar()
        {
            assinatura.Dispose();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            

            var serviceProvider = new ServiceCollection()
                .AddScoped(r => new Regras(limiteFaltas: 3,limieteGols: 1, limiteTempo: 60))
                .AddSingleton<CentralEventos>()
                .AddScoped<Juiz>()
                .BuildServiceProvider();

            var central = serviceProvider.GetService<CentralEventos>();
            var juiz = serviceProvider.GetService<Juiz>();
            var time1 = new Time(central, "Palmeiras");
            var time2 = new Time(central, "Corinthians");

            try
            {
                juiz.IniciarPartida();  
                time1.MarcarGol();  
                time2.CometerFalta();  
                time1.CometerFalta();  
                time2.MarcarGol();  
                time1.CometerFalta();  
                time2.CometerFalta();  
                time2.CometerFalta();  
                time1.MarcarGol();  
            } 
            catch (System.ObjectDisposedException)
            {
                Console.WriteLine("O jogo já acabou");
            }
            

            Console.ReadKey();
        }
    }
}
