using System;
using System.Data.Common;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;
using static PlantsVSZombies.Peashooter;

namespace PlantsVSZombies
{
    public partial class Form1 : Form
    {
        private Zombie selectedZombie;
        private List<Zombie> zombies = new List<Zombie>();

        private const int Rows = 5;
        private const int Columns = 9;
        private const int CellSize = 65;

        private Plant selectedPlant;
        private List<Plant> plants = new List<Plant>();
        private List<Pea> peas = new List<Pea>();

        private SunGenerator _sunGenerator;

        private System.Windows.Forms.Timer gameTimer;
        public Form1()
        {
            InitializeComponent();
            CreateZombieCards();
            CreatePlantCards();
            CreateGameField();
            StartGameTimer();

            // Создание экземпляра класса SunGenerator
            int columns = 9;
            int cellSize = 50;
            int formWidth = this.Width;
            _sunGenerator = new SunGenerator(this, columns, cellSize, formWidth);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void CreatePlantCards()
        {
            // Создание карточек растений
            var peashooterCard = new Button
            {
                Width = 60,
                Height = 80,
                Left = 10,
                Top = 30,
                Image = Properties.Resources.Peashooter
            };
            peashooterCard.Click += (s, e) => SelectPlant(PlantType.Peashooter);
            Controls.Add(peashooterCard);

            // Создание надписи с ценой над карточкой Горохострелом
            var peashooterCostLabel = new Label
            {
                Text = "100",
                Left = peashooterCard.Left,
                Top = peashooterCard.Top - 20
            };
            Controls.Add(peashooterCostLabel);

            var sunflowerCard = new Button
            {
                Width = 60,
                Height = 80,
                Left = 110,
                Top = 30,
                Image = Properties.Resources.Sunflower
            };
            sunflowerCard.Click += (s, e) => SelectPlant(PlantType.Sunflower);
            Controls.Add(sunflowerCard);

            // Создание надписи с ценой над карточкой Подсолнухом
            var sunflowerCostLabel = new Label
            {
                Text = "50",
                Left = sunflowerCard.Left,
                Top = sunflowerCard.Top - 20
            };
            Controls.Add(sunflowerCostLabel);

            // Добавление других карточек растений
        }

        
        private void CreateZombieCards()
        {
            // Создание карточек зомби
            var regularZombieCard = new Button
            {
                Width = 60,
                Height = 80,
                Left = 500,
                Top = 30,
                Image = Properties.Resources.Regular
            };
            regularZombieCard.Click += (s, e) => SelectZombie(ZombieType.Regular);
            Controls.Add(regularZombieCard);

            // Добавление других карточек зомби
        }


        private void SelectZombie(ZombieType zombieType)
        {
            // Выбор зомби для размещения на игровом поле
            selectedZombie = Zombie.Create(zombieType);
        }


        private void SelectPlant(PlantType plantType)
        {
            // Выбор растения для размещения на игровом поле
            selectedPlant = Plant.Create(_sunGenerator, plantType);
        }
        

        private void CreateGameField()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var button = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Left = column * CellSize,
                        Top = row * CellSize + 120
                    };

                    button.Click += (s, e) =>
                    {
                        if (_sunGenerator.SunCurrency >= _sunGenerator.PeashooterCost)
                        {
                            PlacePlant(button);

                            _sunGenerator.SunCurrency -= _sunGenerator.PeashooterCost;
                            _sunGenerator.UpdateSunCurrencyLabel();
                        }

                        if (_sunGenerator.SunCurrency >= _sunGenerator.SunflowerCost)
                        {
                            PlacePlant(button);

                            _sunGenerator.SunCurrency -= _sunGenerator.SunflowerCost;
                            _sunGenerator.UpdateSunCurrencyLabel();
                        }
                        
                        PlaceZombie(button);
                    };

                    Controls.Add(button);                
                }
            }
        }
        

        private void PlacePlant(Button button)
        {
            // Place the selected plant on the game board
            if (selectedPlant != null)
            {
                button.Image = selectedPlant.Image;

                // Determine the row and column of the game board cell
                int row = (button.Top - 100) / CellSize;
                int column = button.Left / CellSize;

                // Add the plant to the list
                plants.Add(Plant.Create(_sunGenerator, selectedPlant.Type, row, column));

                selectedPlant = null;
            }
        }


        private void PlaceZombie(Button button)
        {
            // Установка выбранного зомби на игровое поле
            if (selectedZombie != null)
            {
                button.Image = selectedZombie.Image;

                // Определение координат клетки игрового поля
                int row = (button.Top - 100) / CellSize;
                int column = button.Left / CellSize;

                // Добавление зомби в список
                var zombie = new RegularZombie(row, column);
                zombies.Add(zombie);
                zombie.StartMoving();

                selectedZombie = null;
            }
        }


        private void StartGameTimer()
        {
            gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            gameTimer.Tick += (s, e) => UpdateGame();
            gameTimer.Start();
        }


        private void UpdateGame()
        {
            
            // Перемещение горохов
            foreach (var pea in peas)
            {
                pea.Move();
            }

            peas.RemoveAll(p => p.Column >= Columns);

            // Добавление новых выстрелов из горохомета
            foreach (var peashooter in plants.OfType<Peashooter>())
            {
                var newPeas = peashooter.Shoot(peas);
                peas.AddRange(newPeas);
            }

            // Проверка столкновений между горохами и зомби
            foreach (var pea in peas)
            {
                var zombie = zombies.FirstOrDefault(z => z.Row == pea.Row && z.Column == pea.Column);
                if (zombie != null)
                {
                    // Горох попал в зомби
                    zombie.TakeDamage(pea.Damage);
                    pea.Hit = true;
                    if (zombie.Health <= 0)
                    {
                        zombies.Remove(zombie);

                        // Обновить изображение кнопки
                        var button = (Button)Controls.Find($"Button{zombie.Row}{zombie.Column}", true).FirstOrDefault();
                        if (button != null)
                        {
                            button.Image = null;
                        }
                    }
                }
            }

            peas.RemoveAll(p => p.Hit);

            // Обновление изображений горохов и зомби на игровом поле
            foreach (Control control in Controls)
            {
                if (control is Button button && button.Top >= 100)
                {
                    int row = (button.Top - 100) / CellSize;
                    int column = button.Left / CellSize;
                    var pea = peas.FirstOrDefault(p => p.Row == row && p.Column == column);
                    var zombie = zombies.FirstOrDefault(z => z.Row == row && z.Column == column);
                    var plant = plants.FirstOrDefault(p => p.Row == row && p.Column == column);
                    if (plant != null)
                    {
                        // Не изменять изображение клетки с растением
                    }
                    else if (pea != null)
                    {
                        button.Image = Properties.Resources.Pea;
                    }
                    else if (zombie != null)
                    {
                        button.Image = zombie.Image;
                    }
                    else
                    {
                        button.Image = null;
                    }
                }
            }

            // Проверка столкновений между горохами и зомби
            foreach (var pea in peas)
            {
                var zombie = zombies.FirstOrDefault(z => z.Row == pea.Row && z.Column == pea.Column);
                if (zombie != null)
                {
                    // Горох попал в зомби
                    zombie.TakeDamage(pea.Damage);
                    pea.Hit = true;
                    if (zombie.Health <= 0)
                    {
                        zombies.Remove(zombie);
                    }
                }
            }
        }


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
    
    
    public enum PlantType
    {
        Peashooter,
        Sunflower
    }


    public abstract class Plant
    {
        public PlantType Type { get; }
        public Image Image { get; set; }
        public int Row { get; }
        public int Column { get; }

        protected Plant(PlantType type, int row = -1, int column = -1)
        {
            Type = type;
            Row = row;
            Column = column;
            switch (type)
            {
                case PlantType.Peashooter:
                    Image = Properties.Resources.Peashooter;
                    break;
                case PlantType.Sunflower:
                    Image = Properties.Resources.Sunflower;
                    break;
                // Добавление других типов растений
                default:
                    throw new ArgumentException("Invalid plant type");
            }
        }

        public static Plant Create(SunGenerator sunGenerator, PlantType type, int row = -1, int column = -1)
        {
            switch (type)
            {
                case PlantType.Peashooter:
                    return new Peashooter(row, column);
                case PlantType.Sunflower:
                    return new Sunflower(sunGenerator, row, column);
                // Add other plant types here
                default:
                    throw new ArgumentException("Invalid plant type");
            }
        }
    }


    public class Peashooter : Plant
    {
        private int _shootInterval = 3; // Интервал стрельбы в секундах
        private int _timeSinceLastShot = 0; // Время с последнего выстрела в секундах

        public Peashooter(int row, int column) : base(PlantType.Peashooter, row, column)
        {
        }

        public List<Pea> Shoot(List<Pea> peas)
        {
            var newPeas = new List<Pea>();

            // Создать новый снаряд на следующей клетке через определенный интервал времени
            _timeSinceLastShot++;
            if (_timeSinceLastShot >= _shootInterval)
            {
                newPeas.Add(new Pea(Row, Column + 1));
                _timeSinceLastShot = 0;
            }

            return newPeas;

        }
    }
    
    
    public class Sunflower : Plant
    {
        private System.Timers.Timer _timer;
        public SunGenerator SunGenerator { get; set; }
        public Sunflower(SunGenerator sunGenerator, int row = -1, int column = -1) : base(PlantType.Sunflower, row, column)
        {
            SunGenerator = sunGenerator;

            // Set the image for the sunflower
            Image = Properties.Resources.Sunflower;

            // Create a timer to generate sun every 10 seconds
            _timer = new System.Timers.Timer(10000);
            _timer.Elapsed += GenerateSun;
            _timer.Start();
        }

        private void GenerateSun(object sender, ElapsedEventArgs e)
        {
            SunGenerator.SunCurrency += 15;
        }
    }
    
    
    public class Pea
    {
        public int Damage { get; set; }
        public bool Hit { get; set; }
        public int Column { get; private set; }
        public int Row { get; }

        public Pea(int row, int column)
        {
            Row = row;
            Column = column;
            Damage = 1; // Установить величину урона
        }

        public void Move()
        {
            Column++;
        }
    }

    
    public abstract class Zombie
    {
        private List<Zombie> zombies = new List<Zombie>();
        
        public ZombieType Type { get; }
        private System.Windows.Forms.Timer _timer;
        public Image Image { get; }
        public int Row { get; set; }
        public int Column { get; set; }

        public int Health;

        public void StartMoving()
        {
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 3000; // Движение зомби
            _timer.Tick += Timer_Tick;
            _timer.Start();           
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Move();
        }

        public virtual void Move()
        {
                    
            if (Column == 0)
            {
                MessageBox.Show("Смэрть");

                // Нужно придумать как удалять зомби

                _timer.Stop();                    
            } 
            
            else
            {
                Column--;
            }
        }

        protected Zombie(ZombieType type, int row = -1, int column = -1)
        {
            Type = type;
            Row = row;
            Column = column;
            switch (type)
            {
                case ZombieType.Regular:
                    Image = Properties.Resources.Regular;
                    break;
                    // Добавление новых зомби
            }
        }
        
        public static Zombie Create(ZombieType type, int row = -1, int column = -1)
        {
            switch (type)
            {
                case ZombieType.Regular:
                    return new RegularZombie(row, column);
                // Добавление новых зомби
                default:
                    throw new ArgumentException("Invalid zombie type");
            }
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
        }
    }

    
    public enum ZombieType
    {
        Regular,
        // Добавление других типов зомби
    }

    
    public class RegularZombie : Zombie
    {
        public RegularZombie(int row = -1, int column = -1) : base(ZombieType.Regular, row, column)
        {
            this.Health = 3;
        }
    }

    
    public class SunGenerator
    {
        private Form _form;
        private int _columns;
        private int _cellSize;
        private int _formWidth;
        private System.Windows.Forms.Timer _sunTimer;
        private Random _random = new Random();
        private bool _sunTaken = true;
        public Label _sunCurrencyLabel;
        public int PeashooterCost { get; set; } = 100;
        public int SunflowerCost { get; set; } = 50;

        public int SunCurrency { get; set; }


        public SunGenerator(Form form, int columns, int cellSize, int formWidth)
        {
            _form = form;
            _columns = columns;
            _cellSize = cellSize;
            _formWidth = formWidth;

            // Создание таймера для создания солнца
            _sunTimer = new System.Windows.Forms.Timer();
            _sunTimer.Interval = 5000;
            _sunTimer.Tick += SunTimer_Tick;
            _sunTimer.Start();

            // Создание метки для отображения количества валюты солнца
            _sunCurrencyLabel = new Label();
            _sunCurrencyLabel.AutoSize = false;
            _sunCurrencyLabel.Width = 200;
            _sunCurrencyLabel.Text = "Текущее количество солнца: 0";
            _sunCurrencyLabel.Left = form.Width - _sunCurrencyLabel.Width - 10;
            _sunCurrencyLabel.Top = 10;
            form.Controls.Add(_sunCurrencyLabel);
        }
        public void UpdateSunCurrencyLabel()
        {
            _sunCurrencyLabel.Text = "Текущее количество солнца: " + SunCurrency;
        }
        private void SunTimer_Tick(object sender, EventArgs e)
        {
            if (_sunTaken == true)
            {
                // Создание нового солнца
                var sunButton = new Button();
                sunButton.Image = Properties.Resources.Sun;
                sunButton.Width = 65;
                sunButton.Height = 65;
                sunButton.Click += SunButton_Click;
               
                int gameFieldWidth = _columns * _cellSize;
                int gameFieldLeft = (_formWidth - gameFieldWidth) / 2;
                int sunLeftMin = gameFieldLeft + gameFieldWidth + 10;
                int sunLeftMax = sunLeftMin + 100;
                sunButton.Left = _random.Next(sunLeftMin, sunLeftMax);
                sunButton.Top = 100;

                var moveTimer = new System.Windows.Forms.Timer();
                moveTimer.Interval = 500; 
                moveTimer.Tick += (s, ev) =>
                {
                    sunButton.Top += 25;
                    if (sunButton.Top > (_form.Height - sunButton.Height))
                    {
                        moveTimer.Stop();
                        _form.Controls.Remove(sunButton);
                        _sunTaken = true;
                    }
                };
                
                moveTimer.Start();

                _form.Controls.Add(sunButton);
                _sunTaken = false;
            }
        }

        private void SunButton_Click(object sender, EventArgs e)
        {

            SunCurrency += 25;

            // Обновление количество солнышек
            _sunCurrencyLabel.Text = "Текущее количество солнца: " + SunCurrency;

            // Удаление текстуры солнца после клика
            var sunButton = (Button)sender;
            _form.Controls.Remove(sunButton);
            _sunTaken = true;
        }
    }
}

