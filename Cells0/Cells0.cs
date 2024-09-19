﻿// Cells0.cs
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cells0
{
    /// <summary>
    /// Версія без синхронізації
    /// </summary>
    class Cells0
    {
        // Кількість клітинок
        private int n;

        // Кількість атомів
        private int k;

        // Поріг ймовірності для подальшого руху частинки
        private double p;

        private const int TIME_UNIT_MS = 100;

        // cells[0] - кількість атомів в i-тій клітинці
        private int[] cells;

        // Флаг для зупинки потоків
        private volatile bool running = true;

        // Список завдань для атомів
        private Task[] atomTasks;

        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Використання: Cells0 N K p");
                return;
            }

            Cells0 cells0 = new Cells0(args);
            cells0.StartSimulation();
        }

        public Cells0(string[] args)
        {
            try
            {
                this.n = int.Parse(args[0]);
                this.k = int.Parse(args[1]);
                this.p = double.Parse(args[2]);

                if (n <= 0 || k <= 0 || p < 0 || p > 1)
                {
                    throw new ArgumentException("Некоректні параметри. Переконайтеся, що N та K > 0, 0 ≤ p ≤ 1.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Помилка: {ex.Message}");
                Environment.Exit(1);
            }

            this.cells = new int[n];
            this.cells[0] = k; // Всі атоми спочатку в першій клітинці
        }

        public void StartSimulation()
        {
            Console.WriteLine($"Початок моделювання. Кількість потоків: {k}");
            Console.WriteLine($"Тривалість моделювання: 60 секунд.");
            Console.WriteLine("Моментальні знімки кожну секунду:");

            // Створюємо масив задач
            atomTasks = new Task[k];
            for (int i = 0; i < k; i++)
            {
                atomTasks[i] = Task.Run(() => ParticleRun());
            }

            // Запускаємо знімки
            for (int second = 1; second <= 60; second++)
            {
                Thread.Sleep(1000); // Чекаємо 1 секунду
                PrintSnapshot(second);
            }

            // Зупинка потоків
            running = false;

            try
            {
                Task.WaitAll(atomTasks);
            }
            catch (AggregateException)
            {
                // Ігноруємо виключення, викликані скасуванням задач
            }

            Console.WriteLine("Моделювання завершено.");
            VerifyTotalAtoms();
        }

        public int GetCell(int i)
        {
            if (i >= 0 && i < n)
                return cells[i];
            else
                throw new IndexOutOfRangeException();
        }

        public void MoveParticle(int from, int to)
        {
            cells[from]--;
            cells[to]++;
        }

        private void PrintSnapshot(int second)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{second}s] ");
            for (int i = 0; i < n; i++)
            {
                sb.Append($"{cells[i]} ");
            }
            Console.WriteLine(sb.ToString());
        }

        private void VerifyTotalAtoms()
        {
            int total = 0;
            for (int i = 0; i < n; i++)
            {
                total += cells[i];
            }
            Console.WriteLine($"Початкова кількість атомів: {k}");
            Console.WriteLine($"Кінцева кількість атомів: {total}");
            if (total != k)
            {
                Console.WriteLine("Увага: Загальна кількість атомів змінилася!");
            }
            else
            {
                Console.WriteLine("Загальна кількість атомів залишилася незмінною.");
            }
        }

        private void ParticleRun()
        {
            // Використовуємо окремий екземпляр Random для кожного потоку
            Random random = new Random(Guid.NewGuid().GetHashCode());
            int cell = 0; // Початкова позиція

            while (running)
            {
                double m = random.NextDouble();

                int newPos = cell;
                if (m > p)
                {
                    newPos = cell + 1;
                }
                else
                {
                    newPos = cell - 1;
                }

                // Віддзеркалення на межах
                if (newPos < 0 || newPos >= n)
                {
                    newPos = cell;
                }

                // Оновлення позицій (без блокування)
                MoveParticle(cell, newPos);
                cell = newPos;

                try
                {
                    Thread.Sleep(TIME_UNIT_MS);
                }
                catch (ThreadInterruptedException)
                {
                    // Потік був перерваний
                    break;
                }
            }
        }
    }
}
