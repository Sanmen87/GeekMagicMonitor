using GeekMagicMonitor.Core;
using Microsoft.Win32;

namespace GeekMagicMonitor;

public sealed class MainForm : Form
{
    private Settings settings=Settings.Load();
    private readonly Preview preview=new();
    private readonly TextBox host=new(),location=new(),zone=new();
    private readonly NumericUpDown interval=new(){Minimum=1,Maximum=10},screenSeconds=new(){Minimum=5,Maximum=120},
        latitude=new(){Minimum=-90,Maximum=90,DecimalPlaces=4,Increment=0.1M},longitude=new(){Minimum=-180,Maximum=180,DecimalPlaces=4,Increment=0.1M};
    private readonly ComboBox mode=new(){DropDownStyle=ComboBoxStyle.DropDownList},gpu=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox cities=new(){DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=560};
    private readonly ComboBox weatherProvider=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly CheckBox demo=new(){Text="Демо-данные"},send=new(){Text="Отправлять на куб (HoloClawd)"},
        weatherEnabled=new(){Text="Показывать погоду"},startup=new(){Text="Автозапуск в трее"};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new(730,0)},sensorStatus=new(){AutoSize=true,MaximumSize=new(730,0)};
    private readonly NotifyIcon tray=new(){Icon=AppIcon.Value,Text="GeekMagicMonitor",Visible=true};
    private readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
    private readonly Sensors sensors=new();
    private readonly HttpClient cubeHttp=new(new HttpClientHandler {UseProxy=false,AllowAutoRedirect=false}){Timeout=Timeout.InfiniteTimeSpan};
    private readonly HttpClient weatherHttp=new(){Timeout=TimeSpan.FromSeconds(15)};
    private readonly CubeSender sender;
    private readonly WeatherSource weather;
    private readonly CancellationTokenSource stop=new();
    private Metrics metrics=new();
    private DateTimeOffset lastPoll=DateTimeOffset.MinValue;
    private readonly DateTimeOffset started=DateTimeOffset.UtcNow;
    private Task running=Task.CompletedTask;
    private Task weatherRunning=Task.CompletedTask;
    private Task cityRunning=Task.CompletedTask;
    private bool exiting,closed,changingLanguage,changingSend;
    private readonly bool smokeTest;
    private readonly ComboBox language=new(){DropDownStyle=ComboBoxStyle.DropDownList},displayLanguage=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly Dictionary<Control,string> captions=[];
    private string T(string text)=>Localization.Text(text,settings.Language);
    private void SaveSettings(Settings next) {if(!smokeTest)next.Save();}
    private void TranslateControls(Control parent)
    {
        if(parent is Label or Button or CheckBox or Form && parent!=status && parent!=sensorStatus)
        {
            if(!captions.TryGetValue(parent,out var original))captions[parent]=original=parent.Text;
            parent.Text=T(original);
        }
        foreach(Control child in parent.Controls)TranslateControls(child);
    }
    private void RefreshLanguage()
    {
        TranslateControls(this);
        int index=mode.SelectedIndex;
        mode.Items.Clear();mode.Items.AddRange([T("Авто"),T("Часы и погода"),T("Монитор ПК")]);
        mode.SelectedIndex=index<0?0:index;
        if(gpu.Items.Count>0)gpu.Items[0]=T("Авто");
        weatherProvider.Items[0]=T("Авто (wttr → Open-Meteo)");
        if(tray.ContextMenuStrip is {} menu){menu.Items[0].Text=T("Открыть");menu.Items[1].Text=T("Выход");}
        sensors.Language=settings.Language;
        weather.Language=settings.Language;
        status.Text=T("Настройки сохранены");sensorStatus.Text="";
    }
    private Uri? address;
    private TimeZoneInfo timezone=TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    public MainForm(bool startHidden,string? smokeOutput=null,string smokeLanguage="ru")
    {
        smokeTest=smokeOutput is not null;
        if(smokeOutput is not null){settings=new(){Language=smokeLanguage,DisplayLanguage=smokeLanguage};ShowInTaskbar=false;Opacity=0;}
        Icon=AppIcon.Value;
        sender=new(cubeHttp); weather=new(weatherHttp);
        Text="GeekMagicMonitor — предпросмотр"; ClientSize=new(840,820); MinimumSize=new(780,700);
        var layout=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new(14)};
        Controls.Add(layout);
        var columns=new FlowLayoutPanel {AutoSize=true,WrapContents=false};
        columns.Controls.Add(preview);
        var fields=new FlowLayoutPanel {FlowDirection=FlowDirection.TopDown,WrapContents=false,Width=410,Height=590,AutoScroll=true};
        columns.Controls.Add(fields); layout.Controls.Add(columns);
        void Field(string caption,Control input) { var row=new FlowLayoutPanel{Width=385,Height=31};row.Controls.Add(new Label{Text=caption,Width=170,AutoSize=false});input.Width=195;row.Controls.Add(input);fields.Controls.Add(row); }
        language.Items.AddRange(["Русский","English"]);displayLanguage.Items.AddRange(["Русский","English"]);
        language.SelectedIndex=settings.Language=="en"?1:0;
        displayLanguage.SelectedIndex=settings.DisplayLanguage=="en"?1:0;
        Field("Язык приложения",language);Field("Язык куба",displayLanguage);
        mode.Items.AddRange(["Авто","Часы и погода","PC MONITOR"]);
        gpu.Items.Add("Авто");
        Field("Адрес куба",host);Field("Опрос датчиков, с",interval);Field("Переключение, с",screenSeconds);Field("Экран",mode);Field("GPU",gpu);
        Field("Часовой пояс Windows",zone);Field("Город / поиск",location);
        var findCity=new Button{Text="Найти город",AutoSize=true};fields.Controls.Add(findCity);
        cities.Items.Add(CitySearch.DefaultCity(settings.Language));Field("Выберите результат",cities);
        cities.SelectedIndexChanged+=(_,_)=>
        {
            if(cities.SelectedItem is not CityResult city)return;
            location.Text=city.ToString();latitude.Value=(decimal)city.Latitude;longitude.Value=(decimal)city.Longitude;
            if(TimeZoneInfo.TryConvertIanaIdToWindowsId(city.TimeZone,out var windowsZone))zone.Text=windowsZone;
        };
        findCity.Click+=(_,_)=>cityRunning=FindCityAsync();
        async Task FindCityAsync()
        {
            findCity.Enabled=false;
            try
            {
                string query=location.Text.Split(',')[0];
                var results=await CitySearch.FindAsync(weatherHttp,query,stop.Token,settings.Language);
                cities.Items.Clear();foreach(var city in results)cities.Items.Add(city);
                if(results.Count>0){cities.SelectedIndex=0;cities.DroppedDown=true;status.Text=T("Выберите населённый пункт и нажмите «Применить»");}
                else status.Text=T("Город не найден. Попробуйте другое написание или введите координаты вручную.");
            }
            catch(OperationCanceledException) when(stop.IsCancellationRequested) { }
            catch(Exception ex) when(ex is HttpRequestException or OperationCanceledException or ArgumentException or System.Text.Json.JsonException)
            {cities.Items.Clear();cities.Items.Add(CitySearch.DefaultCity(settings.Language));status.Text=T("Поиск города недоступен. Учкекен доступен в списке без сети; можно ввести координаты вручную.");}
            finally {findCity.Enabled=true;}
        }
        Field("Широта",latitude);Field("Долгота",longitude);
        weatherProvider.Items.AddRange(["Авто (wttr → Open-Meteo)","wttr.in","Open-Meteo"]);
        weatherProvider.SelectedIndex=settings.WeatherProvider switch{"wttr.in"=>1,"Open-Meteo"=>2,_=>0};
        Field("Сервер погоды",weatherProvider);
        foreach(var box in new[]{demo,weatherEnabled,startup,send}) {box.AutoSize=true;fields.Controls.Add(box);}
        var mirrorPreview=new CheckBox{Text="Зеркальный вид LCD (предпросмотр)",AutoSize=true};
        mirrorPreview.CheckedChanged+=(_,_)=>{preview.MirrorHorizontal=mirrorPreview.Checked;preview.Invalidate();};
        fields.Controls.Add(mirrorPreview);
        var apply=new Button{Text="Применить и сохранить",AutoSize=true}; fields.Controls.Add(apply);
        var refreshWeather=new Button{Text="Обновить погоду сейчас",AutoSize=true};fields.Controls.Add(refreshWeather);
        refreshWeather.Click+=(_,_)=>{if(ApplySettings())weather.RetryNow();};
        var adminRestart=new Button{Text="Перезапустить с правами администратора",AutoSize=true,Enabled=!Sensors.IsElevated};
        adminRestart.Click+=(_,_)=>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!)
                {UseShellExecute=true,Verb="runas",Arguments="--wait-parent "+Environment.ProcessId});
                exiting=true;Close();
            }
            catch(System.ComponentModel.Win32Exception){status.Text=T("Повышение прав отменено; приложение продолжает работать без температуры CPU.");}
        };
        layout.Controls.Add(new Label{Text="Предпросмотр 240×240: шрифт схематичный. Состояние отправки сохраняется.",AutoSize=true});
        layout.Controls.Add(status);layout.Controls.Add(sensorStatus);layout.Controls.Add(adminRestart);
        var hide=new Button{Text="Свернуть в трей",AutoSize=true};hide.Click+=(_,_)=>Hide();layout.Controls.Add(hide);
        var quit=new Button{Text="Выход",AutoSize=true};quit.Click+=(_,_)=>{exiting=true;Close();};layout.Controls.Add(quit);
        host.Text=settings.Host;interval.Value=Math.Clamp(settings.Interval,1,10);screenSeconds.Value=Math.Clamp(settings.ScreenSeconds,5,120);
        mode.SelectedIndex=ScreenMode.Normalize(settings.Screen) switch{"Clock"=>1,"Monitor"=>2,_=>0};
        zone.Text=settings.TimeZone;location.Text=settings.Location;
        latitude.Value=(decimal)(double.IsFinite(settings.Latitude)?Math.Clamp(settings.Latitude,-90,90):0);
        longitude.Value=(decimal)(double.IsFinite(settings.Longitude)?Math.Clamp(settings.Longitude,-180,180):0);
        demo.Checked=settings.Demo;weatherEnabled.Checked=settings.WeatherEnabled;gpu.SelectedIndex=0;
        send.Checked=settings.SendToCube;
        using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) startup.Checked=key?.GetValue("GeekMagicMonitor") is not null;
        apply.Click+=(_,_)=>ApplySettings();
        send.CheckedChanged+=(_,_)=>
        {
            if(changingSend)return;
            sender.Reset();
            if(send.Checked)
            {
                if(!ApplySettings())
                {
                    changingSend=true;send.Checked=false;changingSend=false;
                }
            }
            else
            {
                // Stopping and remembering it must work even with invalid, unapplied fields.
                settings=settings with{SendToCube=false};
                try {SaveSettings(settings);}
                catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
                {MessageBox.Show(this,ex.Message,T("Настройки"));}
            }
        };
        var menu=new ContextMenuStrip();menu.Items.Add("Открыть",null,(_,_)=>{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add("Выход",null,(_,_)=>{exiting=true;Close();});
        tray.ContextMenuStrip=menu;tray.DoubleClick+=(_,_)=>{Show();Activate();};
        timer.Tick+=(_,_)=>{if(running.IsCompleted && !exiting)running=TickAsync();};
        Shown+=async (_,_)=>
        {
            ApplyLoadedSettings();timer.Start();running=TickAsync();if(startHidden)Hide();
            if(smokeOutput is not null)
            {
                await running;
                if(preview.Frame is null)throw new InvalidOperationException("Preview did not initialize");
                using var bitmap=new Bitmap(Width,Height);
                DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));
                bitmap.Save(smokeOutput,System.Drawing.Imaging.ImageFormat.Png);
                exiting=true;Close();
            }
        };
        RefreshLanguage();
        language.SelectedIndexChanged+=(_,_)=>ChangeLanguage();
        displayLanguage.SelectedIndexChanged+=(_,_)=>ChangeLanguage();
        void ChangeLanguage()
        {
            if(changingLanguage)return;
            var next=settings with {Language=language.SelectedIndex==1?"en":"ru",DisplayLanguage=displayLanguage.SelectedIndex==1?"en":"ru"};
            changingLanguage=true;
            try {SaveSettings(next);settings=next;RefreshLanguage();sender.Reset();weather.RetryNow();lastPoll=DateTimeOffset.MinValue;}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            {
                language.SelectedIndex=settings.Language=="en"?1:0;
                displayLanguage.SelectedIndex=settings.DisplayLanguage=="en"?1:0;
                status.Text=T("Ошибка: ")+ex.Message;
            }
            finally {changingLanguage=false;}
        }
        if(smokeOutput is not null)
        {
            // Exercise the actual controls and tray translations without saving settings or sending data.
            int originalMode=mode.SelectedIndex;
            mode.SelectedIndex=1;gpu.Items.Add("Test GPU");gpu.SelectedIndex=1;weatherProvider.SelectedIndex=2;
            foreach(int selected in new[]{1,0,1,0})
            {
                language.SelectedIndex=selected;
                if(Text!=Localization.Text("GeekMagicMonitor — предпросмотр",selected==1?"en":"ru") ||
                    mode.SelectedIndex!=1 || gpu.Text!="Test GPU" || weatherProvider.SelectedIndex!=2 ||
                    tray.ContextMenuStrip!.Items[1].Text!=T("Выход"))
                    throw new InvalidOperationException("Language switch changed selection or left untranslated UI");
                foreach(var entry in captions)
                    if(entry.Key.Text!=T(entry.Value))throw new InvalidOperationException("Untranslated control: "+entry.Value);
            }
            mode.SelectedIndex=originalMode;gpu.SelectedIndex=0;gpu.Items.Remove("Test GPU");weatherProvider.SelectedIndex=0;
            language.SelectedIndex=smokeLanguage=="en"?1:0;
            // Real checkbox handlers, with disk/registry writes and network activity suppressed.
            send.Checked=true;
            if(!settings.SendToCube)throw new InvalidOperationException("Enabling sending was not remembered");
            language.SelectedIndex=language.SelectedIndex==0?1:0;
            if(!settings.SendToCube || !send.Checked)throw new InvalidOperationException("Language switch lost sending preference");
            host.Text="invalid/path";
            send.Checked=false;
            if(settings.SendToCube)throw new InvalidOperationException("Disabling sending was not remembered");
            host.Text=settings.Host;
            language.SelectedIndex=smokeLanguage=="en"?1:0;
        }
        FormClosing+=OnClosing;
    }
    private void ApplyLoadedSettings()
    {
        try
        {
            var loadedAddress=CubeSender.Address(settings.Host);
            var loadedZone=TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
            address=loadedAddress;timezone=loadedZone;
        }
        catch(Exception ex) when(ex is ArgumentException or TimeZoneNotFoundException or InvalidTimeZoneException)
        {address=null;status.Text=T("Проверьте настройки: ")+ex.Message;}
    }
    private bool ApplySettings()
    {
        try
        {
            var newAddress=CubeSender.Address(host.Text,settings.Language);
            var newZone=TimeZoneInfo.FindSystemTimeZoneById(zone.Text.Trim());
            var next=settings with {Host=host.Text.Trim(),Interval=(int)interval.Value,ScreenSeconds=(int)screenSeconds.Value,Screen=mode.SelectedIndex switch{1=>"Clock",2=>"Monitor",_=>"Auto"},
                Gpu=gpu.SelectedIndex>0?gpu.Text:"",Demo=demo.Checked,SendToCube=send.Checked,WeatherEnabled=weatherEnabled.Checked,Latitude=(double)latitude.Value,
                Longitude=(double)longitude.Value,Location=location.Text.Trim(),TimeZone=zone.Text.Trim(),
                WeatherProvider=weatherProvider.SelectedIndex switch{1=>"wttr.in",2=>"Open-Meteo",_=>"Auto"}};
            if(next.WeatherEnabled && string.IsNullOrWhiteSpace(next.Location))throw new ArgumentException(T("Укажите город и его координаты"));
            if(!smokeTest)
            {
                using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if(startup.Checked)key.SetValue("GeekMagicMonitor",$"\"{Environment.ProcessPath}\" --tray");else key.DeleteValue("GeekMagicMonitor",false);
            }
            SaveSettings(next);
            settings=next;address=newAddress;timezone=newZone;lastPoll=DateTimeOffset.MinValue;sender.Reset();weather.RetryNow();
            status.Text=T("Настройки сохранены");return true;
        }
        catch(Exception ex) when(ex is ArgumentException or IOException or UnauthorizedAccessException or TimeZoneNotFoundException or InvalidTimeZoneException or System.Security.SecurityException)
        { MessageBox.Show(this,ex.Message,T("Настройки"));return false; }
    }
    private async Task TickAsync()
    {
        try
        {
            var current=settings;var now=DateTimeOffset.UtcNow;
            var frameAddress=address;var frameZone=timezone;
            if(weatherRunning.IsCompleted && !current.Demo)weatherRunning=weather.UpdateAsync(current,stop.Token);
            if((now-lastPoll).TotalSeconds>=Math.Clamp(current.Interval,1,10))
            {
                if(current.Demo)metrics=new(54,28,63,76,12.4,32);
                else
                {
                    metrics=await Task.Run(()=>sensors.Read(current.Gpu),stop.Token);
                    var selected=gpu.Text;
                    foreach(var name in sensors.Gpus)if(!gpu.Items.Contains(name))gpu.Items.Add(name);
                    if(gpu.Items.Contains(current.Gpu))gpu.SelectedItem=current.Gpu;else if(gpu.Items.Contains(selected))gpu.SelectedItem=selected;
                }
                lastPoll=now;
            }
            if(!ReferenceEquals(current,settings))return;
            bool clock=current.Screen=="Clock" || (current.Screen=="Auto" && ((int)(now-started).TotalSeconds/Math.Clamp(current.ScreenSeconds,5,120))%2==0);
            var weatherData=current.Demo?new Weather(18,65,"Cloudy (demo)",now,WindSpeedMps:3.2,WindDirectionDegrees:45):weather.For(current);
            var frame=clock?Screens.Clock(now,frameZone,weatherData,current.Demo,current.Host,current.DisplayLanguage):Screens.Monitor(metrics,current.Demo,now-lastPoll>TimeSpan.FromSeconds(10),current.Host,current.DisplayLanguage);
            preview.Frame=frame;preview.Invalidate();
            sensorStatus.Text=current.Demo?T("ДЕМО: числа условные. Для реальных показаний снимите флажок и примените."):sensors.Detail;
            status.Text=current.SendToCube
                ?frameAddress is not null?await sender.SendAsync(frameAddress,frame,now,stop.Token,current.Language):T("Отправка приостановлена: проверьте адрес куба и часовой пояс.")
                :T("Предпросмотр — отправка на куб выключена");
            if(!current.Demo)status.Text+=" | "+weather.Detail;
        }
        catch(OperationCanceledException) when(stop.IsCancellationRequested) { }
        catch(Exception ex) { Log.Error("tick",ex);status.Text=T("Ошибка: ")+ex.GetType().Name; }
    }
    private async void OnClosing(object? source,FormClosingEventArgs e)
    {
        if(closed)return;
        if(!exiting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();return;}
        e.Cancel=true;exiting=true;timer.Stop();stop.Cancel();await running;
        try {await Task.WhenAll(weatherRunning,cityRunning);} catch(Exception ex){Log.Error("shutdown",ex);}
        try {await Task.Run(sensors.Dispose);} catch(Exception ex){Log.Error("sensor shutdown",ex);}
        tray.Visible=false;tray.Dispose();cubeHttp.Dispose();weatherHttp.Dispose();stop.Dispose();
        closed=true;Close();
    }
}
