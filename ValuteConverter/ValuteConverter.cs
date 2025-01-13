using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ValuteConverterAPICore;

namespace ValuteConverter;

internal class ValuteConverterViewModel : INotifyPropertyChanged
    {

        private readonly ValuteConverterAPI _api;
        private RatesJson _ratesJson;
        public ObservableCollection<string> Currencies { get; }
        private bool costil;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public bool IsAvailable => !_isLoading;

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    LoadCurrencies();
                }
            }
        }

        private string _selectedFromCurrency = "";
        public string SelectedFromCurrency
        {
            get => _selectedFromCurrency;
            set
            {
                if (_selectedFromCurrency != value)
                {
                    _selectedFromCurrency = value;
                    OnPropertyChanged();
                    if(!costil)
                        ConvertCurrenciesFromTo();
                    else
                        costil = false;
                }
            }
        }

        private string _selectedToCurrency = "";
        public string SelectedToCurrency
        {
            get => _selectedToCurrency;
            set
            {
                if (_selectedToCurrency != value)
                {
                    _selectedToCurrency = value;
                    OnPropertyChanged();
                    if(!costil)
                        ConvertCurrenciesToFrom();
                    else
                        costil = false;
                }
            }
        }

        private string _inputAmount = "";
        public string InputAmount
        {
            get => _inputAmount;
            set
            {
                if (_inputAmount != value)
                {
                    _inputAmount = value;
                    OnPropertyChanged();
                    if(!costil)
                        ConvertCurrenciesFromTo();
                    else
                        costil = false;
                }
            }
        }

        private string _convertedAmount = "";
        public string ConvertedAmount
        {
            get => _convertedAmount;
            set
            {
                if (_convertedAmount != value)
                {
                    _convertedAmount = value;
                    OnPropertyChanged();
                    if(!costil)
                        ConvertCurrenciesToFrom();
                    else
                        costil = false;
                }
            }
        }
        
        public ValuteConverterViewModel ()
        {
            _api = new ValuteConverterAPI();
            Currencies = new ObservableCollection<string>();
            LoadCurrencies();
        }

        private async Task LoadCurrencies() { 
            try
            {
                IsLoading = true;

                string lastSelectedFromCurrency = SelectedFromCurrency;
                string lastSelectedToCurrency = SelectedToCurrency;

                _ratesJson = await _api.CallAPI(_selectedDate);

                Currencies.Clear();
                foreach(var rate in _ratesJson.Rates)
                {
                    Currencies.Add($"{rate.Value.Name} ({rate.Key})");
                }

                if (!string.IsNullOrEmpty(lastSelectedFromCurrency))
                    SelectedFromCurrency = Currencies.Where(x => x == lastSelectedFromCurrency).FirstOrDefault();
                else
                    SelectedFromCurrency = Currencies.FirstOrDefault();

                if (!string.IsNullOrEmpty(lastSelectedToCurrency))
                    SelectedToCurrency = Currencies.Where(x => x == lastSelectedToCurrency).FirstOrDefault();
                else
                    SelectedToCurrency = Currencies.FirstOrDefault();

                SelectedDate = _ratesJson.Date;

                IsLoading = false;
                ConvertCurrenciesFromTo();

            } catch(Exception ex)
            {
                Console.WriteLine($"Error loading currencies: {ex.Message}");
            }
        }

        private string GetCurrencyCodeFromCurrenciesEntry(string currencyEntry)
        {
            return currencyEntry.Split('(', ')')[^2];
        }

        private void ConvertCurrenciesFromTo()
        {
            costil = true;
            if (!string.IsNullOrEmpty(SelectedFromCurrency) &&
                !string.IsNullOrEmpty(SelectedToCurrency) &&
                !string.IsNullOrEmpty(InputAmount) && 
                Double.TryParse(InputAmount, out double inputAmountDouble))
            {
                string fromCurrencyCode = GetCurrencyCodeFromCurrenciesEntry(SelectedFromCurrency);
                string toCurrencyCode = GetCurrencyCodeFromCurrenciesEntry(SelectedToCurrency);

                if (_ratesJson.Rates.ContainsKey(fromCurrencyCode) &&
                    _ratesJson.Rates.TryGetValue(toCurrencyCode, out var toCurrency))
                {
                    Valute fromCurrency = _ratesJson.Rates[fromCurrencyCode];

                    double convertedAmountDouble = ConvertValute(fromCurrency, toCurrency, inputAmountDouble);
                    ConvertedAmount = convertedAmountDouble.ToString("F2");
                }
                else
                {
                    ConvertedAmount = string.Empty;
                }
            }
            else
            {
                ConvertedAmount = string.Empty;
            }
        }
        
        private void ConvertCurrenciesToFrom()
        {
            costil = true;
            if (!string.IsNullOrEmpty(SelectedFromCurrency) &&
                !string.IsNullOrEmpty(SelectedToCurrency) &&
                !string.IsNullOrEmpty(ConvertedAmount) && 
                Double.TryParse(ConvertedAmount, out double convertedAmountDouble))
            {
                string fromCurrencyCode = GetCurrencyCodeFromCurrenciesEntry(SelectedFromCurrency);
                string toCurrencyCode = GetCurrencyCodeFromCurrenciesEntry(SelectedToCurrency);

                if (_ratesJson.Rates.ContainsKey(toCurrencyCode) &&
                    _ratesJson.Rates.TryGetValue(fromCurrencyCode, out var fromCurrency))
                {
                    Valute toCurrency = _ratesJson.Rates[toCurrencyCode];

                    double inputAmountDouble = ConvertValute(toCurrency, fromCurrency, convertedAmountDouble);
                    InputAmount = inputAmountDouble.ToString("F2");
                }
                else
                {
                    InputAmount = string.Empty;
                }
            }
            else
            {
                InputAmount = string.Empty;
            }
        }
        
        private double ConvertValute(Valute? fromCurrency, Valute? toCurrency, double inputAmount)
        {
            if (fromCurrency != null && toCurrency != null && inputAmount != 0)
            {
                double fromCurrencyRate = fromCurrency.Value / fromCurrency.Nominal;
                double toCurrencyRate = toCurrency.Value / toCurrency.Nominal;

                double conversionRate = fromCurrencyRate / toCurrencyRate;
                double converted = conversionRate * inputAmount;
                return converted;
            }
            return 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }