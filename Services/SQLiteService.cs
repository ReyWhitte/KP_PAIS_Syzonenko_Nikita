using SQLite;
using AgroGuid.Models.Local;
namespace AgroGuid.Services;
public class SQLiteService {
 private static readonly Lazy<SQLiteAsyncConnection> Shared = new(() => new SQLiteAsyncConnection(
  Path.Combine(FileSystem.AppDataDirectory, "agroguid_offline.db3")));
 private static readonly SemaphoreSlim Gate = new(1, 1);
 private static bool _initialized;
 public SQLiteAsyncConnection Database => Shared.Value;
 public async Task InitializeAsync() {
  if (_initialized) return;
  await Gate.WaitAsync();
  try {
   if (_initialized) return;
   await Database.CreateTableAsync<LocalUser>();
   await Database.CreateTableAsync<LocalField>();
   await Database.CreateTableAsync<LocalCrop>();
   await Database.CreateTableAsync<LocalFieldHistory>();
   if (await Database.Table<LocalCrop>().CountAsync() == 0) {
    foreach (var crop in new [] {
     new LocalCrop { Name="Пшениця", Category="Зернові", Description="Зернова культура, яка використовується для виробництва борошна та круп.", Features="Потребує достатнього освітлення та родючого ґрунту.", CropType="Озимі та ярі сорти" },
     new LocalCrop { Name="Кукурудза", Category="Зернові", Description="Зернова та кормова культура з високою потребою в теплі.", Features="Найкраще розвивається на добре освітлених ділянках.", CropType="Яра культура" },
     new LocalCrop { Name="Соняшник", Category="Технічні", Description="Олійна культура, насіння якої використовується для виробництва олії.", Features="Потребує достатнього освітлення та поживних речовин.", CropType="Яра культура" },
     new LocalCrop { Name="Соя", Category="Технічні", Description="Бобова культура, яка використовується як харчова та кормова сировина.", Features="Має здатність до біологічної фіксації азоту.", CropType="Яра культура" },
     new LocalCrop { Name="Ріпак", Category="Технічні", Description="Олійна культура, що використовується у харчовій промисловості.", Features="Потребує належного зволоження та догляду.", CropType="Озимі та ярі сорти" },
     new LocalCrop { Name="Ячмінь", Category="Зернові", Description="Зернова культура, яка використовується у харчовій промисловості та тваринництві.", Features="Відносно невибагливий до умов вирощування.", CropType="Озимі та ярі сорти" }
    }) await Database.InsertAsync(crop);
   }
   _initialized = true;
  } finally { Gate.Release(); }
 }
}
