using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Device.Location;

namespace Geocaching
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Person>().OwnsOne(p => p.GeoCoordinate)
                .Ignore(gc => gc.Altitude).Ignore(gc => gc.Course)
                .Ignore(gc => gc.HorizontalAccuracy).Ignore(gc => gc.IsUnknown)
                .Ignore(gc => gc.Speed).Ignore(gc => gc.VerticalAccuracy);

            model.Entity<Person>().OwnsOne(p => p.GeoCoordinate, gc => {
                gc.Property(l => l.Longitude).HasColumnName("Longitude");
                gc.Property(l => l.Latitude).HasColumnName("Latitude");
            });
                

            model.Entity<Geocache>().OwnsOne(g => g.GeoCoordinate)
                .Ignore(gc => gc.Altitude).Ignore(gc => gc.Course)
                .Ignore(gc => gc.HorizontalAccuracy).Ignore(gc => gc.IsUnknown)
                .Ignore(gc => gc.Speed).Ignore(gc => gc.VerticalAccuracy);

            model.Entity<Geocache>().OwnsOne(g => g.GeoCoordinate, gc => {
                gc.Property(l => l.Longitude).HasColumnName("Longitude");
                gc.Property(l => l.Latitude).HasColumnName("Latitude");
            });


            model.Entity<FoundGeocache>()
                .HasKey(fg => new { fg.PersonId, fg.GeocacheId });

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Person)
                .WithMany(p => p.FoundGeocaches)
                .HasForeignKey(fg => fg.PersonId);

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Geocache)
                .WithMany(g => g.FoundGeocaches)
                .HasForeignKey(fg => fg.GeocacheId);
        }
    }

    public class Person
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PersonId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string LastName { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Country { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string City { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string StreetName { get; set; }
        public byte StreetNumber { get; set; }

        public GeoCoordinate GeoCoordinate { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        public override string ToString()
        {
            string tmp = this.FirstName + " | " + this.LastName + " | " +
                        this.Country + " | " + this.City + " | " + this.StreetName + " | " +
                        this.StreetNumber + " | " + GeoCoordinate.Latitude + " | " + GeoCoordinate.Longitude;
            return tmp;
        }
    }

    public class Geocache
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeocacheId { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Content { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string Message { get; set; }

        public int? PersonId { get; set; }
        public Person Person { get; set; }

        public GeoCoordinate GeoCoordinate { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

        public override string ToString()
        {
            string tmp = this.GeocacheId + " | " + GeoCoordinate.Latitude.ToString() + " | " +
                            GeoCoordinate.Longitude.ToString() + " | " + this.Content + " | " + this.Message;
            return tmp;
        }
    }

    public class FoundGeocache
    {
        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }

        [ForeignKey("GeocacheId")]
        public int GeocacheId { get; set; }
        public Geocache Geocache { get; set; }

        public static string BuildFoundString(List<FoundGeocache> foundcaches)
        {
            string stringBuilder = "Found: ";
            for (int i = 0; i < foundcaches.Count; i++)
            {
                stringBuilder += foundcaches[i].GeocacheId;
                if (i < foundcaches.Count - 1)
                {
                    stringBuilder += ", ";
                }
            }
            return stringBuilder;
        }
    }

    public partial class MainWindow : Window
    {
        private AppDbContext database = new AppDbContext();
        private const string applicationId = "AlHft3M8psUuZKMImUHduIp_6mnmKRHDIbnRpQr82sfnLC8LS-IZz2vCCF1HTdgi";
        private GeoCoordinate gothenburg = new GeoCoordinate { Latitude = 57.719021, Longitude = 11.991202 };
        private GeoCoordinate geo;
        private GeoCoordinate latestClickLocation;
        private MapLayer layer;
        private Person currentPerson = null;
        Object myLock = new Object();

        public MainWindow()
        {
            latestClickLocation = gothenburg;
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            CreateMap();
        }

        private async void CreateMap()
        {
            try { layer.Children.Clear(); }
            catch { }
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = new Location { Latitude = gothenburg.Latitude, Longitude = gothenburg.Longitude };
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation.Latitude = map.ViewportPointToLocation(point).Latitude;
                latestClickLocation.Longitude = map.ViewportPointToLocation(point).Longitude;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    currentPerson = null;
                    foreach (Pushpin pin in layer.Children)
                    {
                        pin.MouseDown -= ClickGreenButton;
                        pin.MouseDown -= ClickRedButton;
                        try
                        {
                            Geocache geocache = (Geocache)pin.Tag;
                            UpdatePin(pin, Colors.Gray, 1);
                        }
                        catch
                        {
                            Person person = (Person)pin.Tag;
                            UpdatePin(pin, Colors.Blue, 1);
                        }
                    }
                }
            };

            Person[] people = null;
            var getPeople = Task.Run(() =>
            {
                people = database.Person.ToArray();
            });

            await Task.WhenAll(getPeople);

            foreach (Person person in people)
            {
                geo = new GeoCoordinate();
                geo.Longitude = person.GeoCoordinate.Longitude;
                geo.Latitude = person.GeoCoordinate.Latitude;
                string tooptipp = person.FirstName + " " + person.LastName + "\r" + person.StreetName + " " + person.StreetNumber + ", " + person.City;
                var pin = AddPin(geo, tooptipp, Colors.Blue, 1, person);

                pin.MouseDown += PersonClick;
            }

            Geocache[] geocaches = null;
            var getGeocaches = Task.Run(() =>
            {
                geocaches = database.Geocache.Include(g => g.Person).ToArray();
            });

            await Task.WhenAll(getGeocaches);

            foreach (Geocache g in geocaches)
            {
                geo = new GeoCoordinate();
                geo.Longitude = g.GeoCoordinate.Longitude;
                geo.Latitude = g.GeoCoordinate.Latitude;
                string tooltip = g.GeoCoordinate.Latitude + ", " + g.GeoCoordinate.Longitude + "\r" + g.Person.FirstName + " " 
                    + g.Person.LastName + " placerade ut denna geocache med " + g.Content + " i. \r \"" + g.Message + "\"";

                var pin = AddPin(geo, tooltip, Colors.Gray, 1, g);

                pin.MouseDown += Handled;
            }

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private async void ClickGreenButton(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;

            FoundGeocache foundGeocache = await Task.Run(() =>
            {
                return database.FoundGeocache
                    .FirstOrDefault(fg => fg.PersonId == currentPerson.PersonId && fg.GeocacheId == geocache.GeocacheId);
            });

            Task removeFoundGeocache = Task.Run(() =>
            {
                database.Remove(foundGeocache);
                database.SaveChanges();
            });

            await Task.WhenAll(removeFoundGeocache);

            UpdatePin(pin, Colors.Red, 1);
            pin.MouseDown += ClickRedButton;
            pin.MouseDown -= ClickGreenButton;

            e.Handled = true;
        }

        private async void ClickRedButton(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;

            FoundGeocache foundGeocache = new FoundGeocache
            {
                Person = currentPerson,
                Geocache = geocache,
            };

            Task addFoundGeocache = Task.Run(() =>
            {
                database.Add(foundGeocache);
                database.SaveChanges();
            });

            await Task.WhenAll(addFoundGeocache);

            UpdatePin(pin, Colors.Green, 1);
            pin.MouseDown += ClickGreenButton;
            pin.MouseDown -= ClickRedButton;

            e.Handled = true;
        }

        private void Handled(object sender, MouseButtonEventArgs e)
        {
             e.Handled = true;
        }

        private async void PersonClick(object sender, MouseButtonEventArgs e)
        {
            Geocache[] geocaches = null;
            var readGeocaches = Task.Run(() =>
            {
                geocaches = database.Geocache.Select(a => a).ToArray();
            });

            await Task.WhenAll(readGeocaches);

            Pushpin pin = (Pushpin)sender;
            Person person = (Person)pin.Tag;
            string tooltip = pin.ToolTip.ToString();
            currentPerson = person;
            UpdatePin(pin, Colors.Blue, 1);

            foreach (Pushpin pushpin in layer.Children)
            {
                pushpin.MouseDown -= ClickGreenButton;
                pushpin.MouseDown -= ClickRedButton;
                pushpin.MouseDown -= Handled;

                Geocache geocache = geocaches
                    .FirstOrDefault(g => g.GeoCoordinate.Longitude == pushpin.Location.Longitude && g.GeoCoordinate.Latitude == pushpin.Location.Latitude);

                FoundGeocache foundGeocache = null;
                if (geocache != null)
                {
                    foundGeocache = await Task.Run(() => 
                    {
                        return database.FoundGeocache
                           .FirstOrDefault(fg => fg.GeocacheId == geocache.GeocacheId && fg.PersonId == person.PersonId);
                    });
                }

                if (geocache == null && pushpin.ToolTip.ToString() != tooltip)
                {
                    UpdatePin(pushpin, Colors.Blue, 0.5);
                }
                else if (geocache != null && geocache.PersonId == person.PersonId)
                {
                    UpdatePin(pushpin, Colors.Black, 1);
                    pushpin.MouseDown += Handled;
                }
                else if (geocache != null && foundGeocache != null)
                {
                    UpdatePin(pushpin, Colors.Green, 1);
                    pushpin.MouseDown += ClickGreenButton;
                }
                else if (geocache != null && foundGeocache == null)
                {
                    UpdatePin(pushpin, Colors.Red, 1);
                    pushpin.MouseDown += ClickRedButton;
                }

                e.Handled = true;
            }
        }

        private async void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            if (currentPerson != null)
            {
                GeocacheDialog dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();
                if (dialog.DialogResult == false) { return; }

                string contents = dialog.GeocacheContents;
                string message = dialog.GeocacheMessage;

                Geocache geocache = new Geocache
                {
                    Content = contents,
                    Message = message,
                    Person = currentPerson,
                };

                geocache.GeoCoordinate.Latitude = latestClickLocation.Latitude;
                geocache.GeoCoordinate.Longitude = latestClickLocation.Longitude;

                Task addGeocache = Task.Run(() =>
                {
                    database.Add(geocache);
                    database.SaveChanges();
                });

                await Task.WhenAll(addGeocache);
                geo = new GeoCoordinate();
                geo.Longitude = geocache.GeoCoordinate.Longitude;
                geo.Latitude = geocache.GeoCoordinate.Latitude;

                string tooltip = geocache.GeoCoordinate.Latitude + ", " + geocache.GeoCoordinate.Longitude + "\r" + geocache.Person.FirstName 
                    + " " + geocache.Person.LastName + " placerade ut denna geocache med " + geocache.Content + " i. \r \"" + geocache.Message + "\"";

                var pin = AddPin(geo, tooltip, Colors.Black, 1, geocache);
                pin.MouseDown += Handled;
            }
            else
            {
                MessageBox.Show("Please select a person before creating a geocache.");
            }
        }

        private async void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            PersonDialog dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false) { return; }

            string firstName = dialog.PersonFirstName;
            string lastName = dialog.PersonLastName;
            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            byte streetNumber = dialog.AddressStreetNumber;

            Person person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                City = city,
                Country = country,
                StreetName = streetName,
                StreetNumber = streetNumber,
            };

            person.GeoCoordinate.Longitude = latestClickLocation.Longitude;
            person.GeoCoordinate.Latitude = latestClickLocation.Latitude;

            Task addPerson = Task.Run(() =>
            {
                database.Add(person);
                database.SaveChanges();
            });

            await Task.WhenAll(addPerson);

            geo = new GeoCoordinate();
            geo.Longitude = person.GeoCoordinate.Longitude;
            geo.Latitude = person.GeoCoordinate.Latitude;

            string tooptip = person.FirstName + " " + person.LastName + "\r" + person.StreetName + " " + person.StreetNumber + ", " + person.City;

            var pin = AddPin(geo, tooptip, Colors.Blue, 1, person);

            currentPerson = person;

            pin.MouseDown += PersonClick;
        }

        private Pushpin AddPin(GeoCoordinate geo, string tooltip, Color color, double opacity, object o)
        {
            Location location = new Location { Longitude = geo.Longitude, Latitude = geo.Latitude };
            Pushpin pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
            pin.Location = location;
            pin.Tag = o;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, location);
            return pin;
        }

        private void UpdatePin(Pushpin pin, Color color, double opacity)
        {
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
        }

        private async void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true) { return; }

            string path = dialog.FileName;

            Task LoadToDatabase = Task.Run(() =>
            {
                database.Person.RemoveRange(database.Person);
                database.Geocache.RemoveRange(database.Geocache);
                database.FoundGeocache.RemoveRange(database.FoundGeocache);
                database.SaveChanges();
            });

            List<List<string>> collection = new List<List<string>>();
            List<string> linesWithObjects = new List<string>();

            List<Person> people = new List<Person>();
            Person person;
            List<Geocache> geocaches = new List<Geocache>();
            Geocache geocache;

            Dictionary<string[], Person> pickedGeocaches = new Dictionary<string[], Person>();
            Dictionary<int, Geocache> geoWithTextfileId = new Dictionary<int, Geocache>();

            string[] textfileLines = File.ReadAllLines(path).ToArray();

            Task.WaitAll(LoadToDatabase);

            foreach (string line in textfileLines)
            {
                if(line != "")
                {
                    linesWithObjects.Add(line);
                    continue;
                }
                else
                {
                    collection.Add(linesWithObjects);
                    linesWithObjects = new List<string>();
                }
            }
            collection.Add(linesWithObjects);

            for (int i = 0; i < collection.Count(); i++)
            {
                string[] onePersonsInfo = collection[i][0].Split('|').Select(v => v.Trim()).ToArray();

                person = new Person
                {
                    FirstName = onePersonsInfo[0],
                    LastName = onePersonsInfo[1],
                    Country = onePersonsInfo[2],
                    City = onePersonsInfo[3],
                    StreetName = onePersonsInfo[4],
                    StreetNumber = byte.Parse(onePersonsInfo[5]),
                    GeoCoordinate = new GeoCoordinate
                    {
                        Latitude = double.Parse(onePersonsInfo[6]),
                        Longitude = double.Parse(onePersonsInfo[7]),
                    }
                };
                people.Add(person);

                for (int k = 1; k < collection[i].Count(); k++)
                {
                    try
                    {
                        string[] tmp = collection[i][k].Split('|').Select(v => v.Trim()).ToArray();
                        geocache = new Geocache
                        {
                            Content = tmp[3],
                            Message = tmp[4],
                            Person = person,
                            GeoCoordinate = new GeoCoordinate
                            {
                                Latitude = double.Parse(tmp[1]),
                                Longitude = double.Parse(tmp[2]),
                            }
                        };

                        geoWithTextfileId.Add(int.Parse(tmp[0]), geocache);
                        geocaches.Add(geocache);

                        Task AddToDatabase= Task.Run(() =>
                        {
                            database.Add(person);
                            database.Add(geocache);
                        });
                        await Task.WhenAll(AddToDatabase);
                    }
                    // If we can't create a geocache object from current line, we know that the line "contains" FoundGeocaches,
                    // and we will end up in this catch statement.
                    catch
                    {
                        string[] numbers = collection[i][k].Remove(0, 6).Split(',').Select(v => v.Trim()).ToArray();
                        if (!numbers.Contains("")) pickedGeocaches.Add(numbers, person);
                    }
                }
            }

            var addFoundGeocaches = Task.Run(() =>
            {
                pickedGeocaches.Select(pair => pair).ToList()
                    .ForEach(entry =>
                        entry.Key.Select(k => k).ToList().ForEach(key =>
                            database.Add(new FoundGeocache { Person = entry.Value, Geocache = geoWithTextfileId.FirstOrDefault(g => g.Key == (int.Parse(key))).Value })
                ));
                database.SaveChanges();
            });

            await Task.WhenAll(addFoundGeocaches);
            CreateMap();
        }

        private async void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true) return;

            string path = dialog.FileName;

            List<string> linesToTextfile = new List<string>();

            var readToFile = Task.Run(() =>
            {
                Person[] people = database.Person.Select(p => p).OrderByDescending(a => a).ToArray();
                lock(myLock)
                {
                    foreach (Person person in people)
                    {
                        linesToTextfile.Add(person.ToString());

                        List<Geocache> geocaches = database.Geocache.
                            Where(g => g.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToList();

                        geocaches.ForEach(g => linesToTextfile.Add(g.ToString()));

                        List<FoundGeocache> foundcaches = database.FoundGeocache.
                            Where(f => f.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToList();

                        linesToTextfile.Add(FoundGeocache.BuildFoundString(foundcaches));
                        linesToTextfile.Add("");
                    }
                }
                linesToTextfile.RemoveAt(linesToTextfile.Count()-1);
            });
            await Task.WhenAll(readToFile);
            File.WriteAllLines(path, linesToTextfile);
        }

        private void ExitMap(object sender, RoutedEventArgs args)
        {
            MessageBox.Show("Thank you for Geocaching with us, see you another time!");
            Environment.Exit(0);
        }
    }
}