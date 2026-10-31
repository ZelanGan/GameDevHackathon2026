using System.IO;
using NightTrolleybus.Items;
using NightTrolleybus.Narrative;
using UnityEditor;
using UnityEngine;
using static NightTrolleybus.EditorTools.BuilderTools;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Створює асети реплік і подій. Наявні не перезаписує, тому правки в редакторі зберігаються.</summary>
    public static class NarrativeContent
    {
        private const string LinesPath = Root + "/Data/Dialogues";
        private const string EventsPath = Root + "/Data/Events";

        public static Object[] Lines() => new Object[]
        {
            Line("Bright_1", DialogueTrigger.BecameBright, "Тепло! Я бачу аж до кінця салону!"),
            Line("Bright_2", DialogueTrigger.BecameBright, "Ось так. Тепер темрява відступить хоч на крок."),
            Line("Weak_1", DialogueTrigger.BecameWeak, "Мені стає важко світити..."),
            Line("Weak_2", DialogueTrigger.BecameWeak, "Знайди щось, що горить. Будь ласка."),
            Line("Critical_1", DialogueTrigger.BecameCritical, "Я... майже не відчуваю себе."),
            Line("Critical_2", DialogueTrigger.BecameCritical, "Не дай мені згаснути. Свічку. Сірник. Хоч щось!"),
            Line("Fuel_1", DialogueTrigger.FuelReceived, "О-о-о, смачно!"),
            Line("Fuel_2", DialogueTrigger.FuelReceived, "Дякую. Я знову тут."),
            Line("Fuel_Low", DialogueTrigger.FuelReceived, "Ти встиг. Ще трохи, і було б пізно.", 0f, 30f),
            Line("Cold_1", DialogueTrigger.ColdInCabin, "Холодно... Увімкни обігрівач або зачини двері."),
            Line("Cold_2", DialogueTrigger.ColdInCabin, "Цей холод їсть мене швидше за темряву."),
            Line("Fear_1", DialogueTrigger.Frightened, "Там щось є. Воно дивиться на мене."),
            Line("Fear_2", DialogueTrigger.Frightened, "Не підпускай їх до мене. Мені страшно!"),
            Line("Flash_1", DialogueTrigger.Flash, "Геть від нас!"),
            Line("Flash_2", DialogueTrigger.Flash, "Ох... Це забрало багато сили."),
            Line("PowerOut_1", DialogueTrigger.PowerOut, "Тролейбус заснув. Йому потрібна енергія."),
            Line("Broken_1", DialogueTrigger.BusBroken, "Щось клацнуло. Глянь, де блимає червоне."),
            Line("LeftBus_1", DialogueTrigger.LeftBus, "Візьми мене з собою. Сам я тут не впораюся."),
            Line("LeftBus_2", DialogueTrigger.LeftBus, "Не затримуйся. Темрява вже знає, що ми тут."),
            Line("Route_Depot", DialogueTrigger.RouteDepot, "Депо? Там мають бути дроти з живою напругою."),
            Line("Route_Market", DialogueTrigger.RouteSupermarket, "Там багато свічок. І не лише свічок. Обережно."),
            Line("Route_Gun", DialogueTrigger.RouteGunShop, "Зброя мене не гріє. Але тебе, може, вбереже."),
            Line("Route_Event", DialogueTrigger.RouteEvent, "Туди я ще не дивився. Побачимо, що чекає."),
            Line("Route_Empty", DialogueTrigger.RouteEmptyRoad, "Порожня дорога. Тихо. Майже добре."),
            Line("Route_Final", DialogueTrigger.RouteFinal, "Я це відчуваю. Кінцева зовсім близько."),
        };

        public static RoadEventData Intro() => Event("Intro", "Нічний рейс",
            "Згасли ліхтарі. Потім вікна. Потім зорі.\nНаприкінці зміни ти помічаєш на сидінні крихітний живий вогник.\n«Довези мене до кінцевої, — шепоче він. — Там ще можна повернути світло. Тільки не дай мені згаснути».",
            new RoadEventChoice { text = "Сідай ближче. Поїхали", outcome = "Вогник тремтить і трохи яскравішає. Попереду ніч і ніякого розкладу." });

        public static Object[] Events()
        {
            ItemData candle = Candle;
            ItemData match = Match;
            ItemData battery = Battery;
            ItemData ticket = Ticket;

            return new Object[]
            {
                Event("Conductor", "Стара кондукторка",
                    "На порожній зупинці стоїть кондукторка в формі давно закритого депо. Вона простягає руку: «Ваш квиток?»",
                    new RoadEventChoice { text = "Віддати квиток", requiredItem = ticket, rewardItem = battery, outcome = "Вона пробиває квиток і мовчки ставить біля дверей акумулятор." },
                    new RoadEventChoice { text = "Проїхати повз", flameChange = -5f, outcome = "Її погляд проводжає вас довго. Вогник чомусь зіщулюється." }),
                Event("Lantern", "Ліхтар, що ще дихає",
                    "Один ліхтар на вулиці ще тьмяно жевріє. Вогник тягнеться до нього.",
                    new RoadEventChoice { text = "Дати Вогнику забрати це світло", flameChange = 20f, outcome = "Ліхтар гасне назавжди. Вогник яскравіє й довго мовчить." },
                    new RoadEventChoice { text = "Залишити ліхтар світити", temperatureChange = 8f, outcome = "Ви вмикаєте фари й сигналите йому на прощання. Чомусь у салоні стає тепліше." }),
                Event("Sparks", "Обірваний дріт",
                    "Над дорогою гойдається дріт і сиплеться іскрами. Струм ще є.",
                    new RoadEventChoice { text = "Під'єднатися на ходу", powerChange = 25f, flameChange = -8f, outcome = "Є заряд! Але розряд боляче вдарив і по Вогнику." },
                    new RoadEventChoice { text = "Об'їхати", powerChange = -5f, outcome = "Об'їзд коштує трохи енергії, зате всі цілі." }),
                Event("Child", "Дитина без тіні",
                    "Посеред дороги стоїть дитина. Світло фар проходить крізь неї, і тіні немає.",
                    new RoadEventChoice { text = "Зупинитися й відчинити двері", temperatureChange = -15f, rewardItem = candle, outcome = "У салон заходить холод, а дитини немає. На сидінні лежить свічка." },
                    new RoadEventChoice { text = "Натиснути на сигнал і їхати", flameChange = -6f, outcome = "Дитина не зрушила з місця. Ви проїхали крізь неї. Вогник не каже ні слова." }),
                Event("Kiosk", "Кіоск «Преса»",
                    "Кіоск відчинений, а всередині нікого. На прилавку сірники, поруч пачка старих квитків.",
                    new RoadEventChoice { text = "Взяти сірники", rewardItem = match, outcome = "Сірники сухі. Це вже удача." },
                    new RoadEventChoice { text = "Взяти квитки", rewardItem = ticket, outcome = "Кажуть, ними ще можна розрахуватися. Хтось досі їх приймає." }),
                Event("Choir", "Хор у темряві",
                    "З темряви долинає спів. Вогник розгойдується в такт і наче засинає.",
                    new RoadEventChoice { text = "Заглушити спів сиреною", powerChange = -8f, outcome = "Сирена рве тишу. Спів обривається, Вогник здригається й прокидається." },
                    new RoadEventChoice { text = "Послухати ще трохи", flameChange = -12f, temperatureChange = 6f, outcome = "Стає дивно затишно. Коли ви отямились, полум'я стало значно меншим." }),
                Event("Mechanic", "Механік із ліхтариком",
                    "На узбіччі механік махає згаслим ліхтариком: «Поділіться вогнем, а я підкручу вам контакти».",
                    new RoadEventChoice { text = "Віддати сірник", requiredItem = match, powerChange = 20f, outcome = "Механік щасливий. Тролейбус гуде бадьоріше." },
                    new RoadEventChoice { text = "Дати Вогнику поділитися", flameChange = -10f, powerChange = 30f, outcome = "Вогник простягає іскру. Йому важко, але механік не обманює." },
                    new RoadEventChoice { text = "Їхати далі", outcome = "Ліхтарик ще довго блимає в дзеркалі, поки не зникає." }),
                Event("Fog", "Чорний туман",
                    "Дорогу перетинає туман, густий, як сажа. Об'їзду не видно.",
                    new RoadEventChoice { text = "Їхати напролом", flameChange = -10f, outcome = "Туман лиже вікна. Вогник притискається до сидіння, але ви проходите." },
                    new RoadEventChoice { text = "Вибити туман обігрівачем на повну", powerChange = -15f, temperatureChange = 15f, outcome = "Тепло розганяє темряву. Акумулятор це відчув." }),
                Event("Bakery", "Пекарня",
                    "Із вікна пекарні ще йде тепло, хоч у ній давно нікого немає.",
                    new RoadEventChoice { text = "Постояти біля стіни", temperatureChange = 25f, outcome = "Тепла стіна й запах хліба. На кілька хвилин усе майже добре." },
                    new RoadEventChoice { text = "Пошукати всередині", rewardItem = candle, flameChange = -4f, outcome = "У шафі знайшлася свічка. Поки шукали, Вогник чекав сам і трохи згас." }),
            };
        }

        private static DialogueLine Line(string assetName, DialogueTrigger trigger, string text, float minFlame = 0f, float maxFlame = 100f)
        {
            string path = $"{LinesPath}/{assetName}.asset";
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<DialogueLine>(path);

            var line = GetOrCreate<DialogueLine>(path);
            SetEnum(line, "trigger", (int)trigger);
            SetString(line, "text", text);
            SetFloat(line, "minFlame", minFlame);
            SetFloat(line, "maxFlame", maxFlame);
            return line;
        }

        private static RoadEventData Event(string assetName, string title, string description, params RoadEventChoice[] choices)
        {
            string path = $"{EventsPath}/{assetName}.asset";
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<RoadEventData>(path);

            var roadEvent = GetOrCreate<RoadEventData>(path);
            SetString(roadEvent, "title", title);
            SetString(roadEvent, "description", description);

            var serialized = new SerializedObject(roadEvent);
            SerializedProperty array = serialized.FindProperty("choices");
            array.arraySize = choices.Length;
            for (int i = 0; i < choices.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("text").stringValue = choices[i].text;
                element.FindPropertyRelative("outcome").stringValue = choices[i].outcome;
                element.FindPropertyRelative("requiredItem").objectReferenceValue = choices[i].requiredItem;
                element.FindPropertyRelative("rewardItem").objectReferenceValue = choices[i].rewardItem;
                element.FindPropertyRelative("flameChange").floatValue = choices[i].flameChange;
                element.FindPropertyRelative("temperatureChange").floatValue = choices[i].temperatureChange;
                element.FindPropertyRelative("powerChange").floatValue = choices[i].powerChange;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(roadEvent);
            return roadEvent;
        }
    }
}
