using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        private AppDbContext database = new AppDbContext();
        private const string applicationId = "AlHft3M8psUuZKMImUHduIp_6mnmKRHDIbnRpQr82sfnLC8LS-IZz2vCCF1HTdgi";
        private GeoCoordinate gothenburg = new GeoCoordinate { Latitude = 57.719021, Longitude = 11.991202 };
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
            if(map.Center.Latitude == 0 || map.Center.Longitude == 0)
            {
                map.Center = new Location
                {
                    Latitude = gothenburg.Latitude,
                    Longitude = gothenburg.Longitude
                };
                map.ZoomLevel = 12;
            }
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
                var pin = AddPin(person.GeoCoordinate, person.GetTooltipMessage(), Colors.Blue, 1, person);
                pin.MouseDown += PersonClick;
            }

            Geocache[] geocaches = null;
            var getGeocaches = Task.Run(() =>
            {
                geocaches = database.Geocache.Include(g => g.Person).ToArray();
            });

            await Task.WhenAll(getGeocaches);

            foreach (Geocache geocache in geocaches)
            {
                var pin = AddPin(geocache.GeoCoordinate, geocache.GetTooltipMessage(), Colors.Gray, 1, geocache);
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

                Geocache geocache = new Geocache
                {
                    Content = dialog.GeocacheContents,
                    Message = dialog.GeocacheMessage,
                    Person = currentPerson,
                    GeoCoordinate = new GeoCoordinate
                    {
                        Latitude = latestClickLocation.Latitude,
                        Longitude = latestClickLocation.Longitude,
                    }
                };

                Task addGeocache = Task.Run(() =>
                {
                    database.Add(geocache);
                    database.SaveChanges();
                });

                await Task.WhenAll(addGeocache);

                var pin = AddPin(geocache.GeoCoordinate, geocache.GetTooltipMessage(), Colors.Black, 1, geocache);
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

            Person person = new Person
            {
                FirstName = dialog.PersonFirstName,
                LastName = dialog.PersonLastName,
                City = dialog.AddressCity,
                Country = dialog.AddressCountry,
                StreetName = dialog.AddressStreetName,
                StreetNumber = dialog.AddressStreetNumber,
                GeoCoordinate = new GeoCoordinate
                {
                    Latitude = latestClickLocation.Latitude,
                    Longitude = latestClickLocation.Longitude
                },
            };

            Task addPerson = Task.Run(() =>
            {
                database.Add(person);
                database.SaveChanges();
            });

            await Task.WhenAll(addPerson);

            var pin = AddPin(person.GeoCoordinate, person.GetTooltipMessage(), Colors.Blue, 1, person);

            currentPerson = person;

            pin.MouseDown += PersonClick;
        }

        private Pushpin AddPin(GeoCoordinate geo, string tooltip, Color color, double opacity, object o)
        {
            Pushpin pin = new Pushpin
            {
                Tag = o,
                Opacity = opacity,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(color),
                Location = new Location
                {
                    Latitude = geo.Latitude,
                    Longitude = geo.Longitude
                },
            };
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, pin.Location);
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
                        Longitude = double.Parse(onePersonsInfo[7])
                    },
                };
                people.Add(person);
                await database.AddAsync(person);

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
                                Longitude = double.Parse(tmp[2])
                            },
                        };

                        geoWithTextfileId.Add(int.Parse(tmp[0]), geocache);
                        geocaches.Add(geocache);

                        Task AddToDatabase= Task.Run(() =>
                        {
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
                lock(myLock)
                {
                    Person[] people = database.Person.Select(p => p).OrderByDescending(a => a).ToArray();
                    foreach (Person person in people)
                    {
                        linesToTextfile.Add(person.ToCsvFormat());

                        List<Geocache> geocaches = database.Geocache.
                            Where(g => g.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToList();

                        geocaches.ForEach(g => linesToTextfile.Add(g.ToCsvFormat()));

                        List<FoundGeocache> foundcaches = database.FoundGeocache.
                            Where(f => f.PersonId == person.PersonId).
                            OrderByDescending(a => a).ToList();

                        linesToTextfile.Add(FoundGeocache.ToCsvFormat(foundcaches));
                        linesToTextfile.Add("");
                    }
                    linesToTextfile.RemoveAt(linesToTextfile.Count() - 1);
                }
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