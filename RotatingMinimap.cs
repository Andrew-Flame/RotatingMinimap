using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimMod
{
    [BepInPlugin("Flame.RotatingMinimap", "Rotating Minimap", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class RotatingMinimap : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("Flame.RotatingMinimap");
        private static Texture2D maskTexture = new Texture2D(1000, 1000);
        private static Texture2D compassTexture = new Texture2D(1000, 1000);
        private static GameObject minimap;
        private static GameObject pinRoot;

        void Awake()
        {
            harmony.PatchAll();

            /* Подгружаем текстуры */
            ImageConversion.LoadImage(maskTexture, File.ReadAllBytes("./BepInEx/plugins/RotatingMinimap/Mask.png"));  //Подгружаем маску из файла
            ImageConversion.LoadImage(compassTexture, File.ReadAllBytes("./BepInEx/plugins/RotatingMinimap/RectCompass.png"));  //Подгружаем компас из файла
        }

        /* Этот код не позволяем маркеру игрока на миникарте крутиться */
        [HarmonyPatch(typeof(Minimap), "UpdatePlayerMarker")]
        class PlayerMarkerPatch
        {
            private static void Postfix(ref RectTransform ___m_smallMarker, ref RectTransform ___m_smallShipMarker)
            {
                ___m_smallMarker.rotation = Quaternion.Euler(0f, 0f, 0f);  //Делаем так, чтобы маркер игрока не разворачивался
                ___m_smallShipMarker.Rotate(0f, 0f, GameCamera.instance.transform.rotation.eulerAngles.y);  //Поворачиваем метку корабля
            }
        }

        /* Этот код разворачивает карту при каждом ее обновлении */
        [HarmonyPatch(typeof(Minimap), "UpdateMap")]
        class MinimapPatch
        {
            private static void Postfix()
            {
                minimap.transform.rotation = Quaternion.Euler(0f, 0f, GameCamera.instance.transform.rotation.eulerAngles.y);  //Разворачиваем миникарту и компас
                /* Перебираем пины на миникарте циклом и разворачиваем */
                foreach (Transform pin in pinRoot.GetComponentInChildren<Transform>())
                    pin.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        [HarmonyPatch(typeof(Minimap), "Awake")]
        class MinimapAppearancePatch
        {
            private static void Postfix(ref GameObject ___m_smallRoot)
            {
                minimap = ___m_smallRoot.transform.Find("map").gameObject;  //Инициализируем объект миникарты, которыйбудем крутить в будущем
                pinRoot = minimap.transform.Find("pin_root").gameObject;  //Инициализируем объект, хранящий в себе пины на миникарте
                float size = ___m_smallRoot.GetComponent<RectTransform>().sizeDelta.x;  //Получаем размер, под  которыйбудем подгонять размеры нашего интерфейса

                /* Избавляемся от объектов и компонентов, которые будут нам мешать */
                ___m_smallRoot.transform.Find("biome").gameObject.SetActive(false);
                minimap.transform.Find("wind_marker").gameObject.SetActive(false);
                Destroy(___m_smallRoot.GetComponent<RectMask2D>());
                Destroy(minimap.GetComponent<RectMask2D>());

                /* Создаем круглую маску */
                ___m_smallRoot.GetComponent<Image>().sprite = Sprite.Create(maskTexture, new Rect(0, 0, 1000, 1000), new Vector2(0, 0));  //Создаем спрайт маски из текстуры
                //___m_smallRoot.AddComponent<Mask>();  //Добавляем компонент маски

                /* Создаем объект компаса и добавляем его к миникарте */
                GameObject compass = new GameObject("Сompass");  //Инициализируем объект
                Image compassImage = compass.AddComponent<Image>();  //Инициализируем компонент изображения компаса
                compassImage.sprite = Sprite.Create(compassTexture, new Rect(0, 0, 1000, 1000), new Vector2(0, 0));  //Создаем спрайт компаса из текстуры
                compassImage.maskable = false;  //Делаем так, чтобы маска никак не влияла на изображение компаса
                compass.transform.SetParent(minimap.transform);  //Делаем нашу миникарту родителем для компаса, чтобы он вращался  вместе с ней
                compass.transform.localPosition = new Vector3(0f, 0f, 0f);  //Перемещаем объект в начало координат
                compass.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);  //Редактируем размер в соответствии со стандартами
                Instantiate(compass);  //Загружаем компас
            }
        }
    }
}