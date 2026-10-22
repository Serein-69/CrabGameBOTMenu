using System;
using UnityEngine;
using UnityEngine.UI;
using SteamworksNative;

namespace BOTMenuMod
{
    public class BOTDisclaimerUI : MonoBehaviour
    {
        public static BOTDisclaimerUI Instance;
        private static bool _created;
        private GameObject _panel;
        private RectTransform _panelRect;
        private Text _titleText;
        private Text _bodyText;
        private Button _agreeBtn;
        private Button _declineBtn;

        public static bool IsAccepted
        {
            get => PlayerPrefs.GetInt("BOT_Disclaimer_Accepted", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("BOT_Disclaimer_Accepted", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public BOTDisclaimerUI(IntPtr ptr) : base(ptr) { }

        public static void Create(Transform parent)
        {
            if (_created) return;
            GameObject go = new GameObject("BOT_DisclaimerUI");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<BOTDisclaimerUI>();
            Instance.Init();
            _created = true;
        }

        private void Init()
        {
            _panel = this.gameObject;
            _panelRect = _panel.AddComponent<RectTransform>();
            _panelRect.anchorMin = _panelRect.anchorMax = _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.sizeDelta = new Vector2(580, 440);

            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.05f, 0.98f);

            var outline = _panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.6f, 0.2f, 1f, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_panel.transform, false);
            _titleText = titleObj.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = 20;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = Color.white;
            _titleText.supportRichText = true;

            RectTransform titleRt = _titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -15);
            titleRt.sizeDelta = new Vector2(-40, 35);

            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(_panel.transform, false);
            _bodyText = bodyObj.AddComponent<Text>();
            _bodyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _bodyText.fontSize = 13;
            _bodyText.lineSpacing = 1.25f;
            _bodyText.alignment = TextAnchor.UpperLeft;
            _bodyText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            _bodyText.supportRichText = true;

            RectTransform bodyRt = _bodyText.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.offsetMin = new Vector2(25, 75);
            bodyRt.offsetMax = new Vector2(-25, -60);

            GameObject btnContainer = new GameObject("Btns");
            btnContainer.transform.SetParent(_panel.transform, false);
            RectTransform btnRt = btnContainer.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0, 0);
            btnRt.anchorMax = new Vector2(1, 0);
            btnRt.pivot = new Vector2(0.5f, 0);
            btnRt.anchoredPosition = new Vector2(0, 20);
            btnRt.sizeDelta = new Vector2(-50, 42);

            _agreeBtn = CreateButton(btnContainer.transform, "AgreeBtn", new Vector2(-130, 0), new Color(0.1f, 0.6f, 0.2f, 0.95f), () => {
                IsAccepted = true;
                _panel.SetActive(false);
                Utility.ForceMessage("<color=#00FF44>Agreement Accepted! Welcome.</color>");

                if (!BOTPlugin.ConfigIsNameSet.Value)
                {
                    if (BOTNameSetup.Instance != null) BOTNameSetup.Instance.Show();
                }
                else
                {
                    Variables.menuTrigger = true;
                }
            });

            _declineBtn = CreateButton(btnContainer.transform, "DeclineBtn", new Vector2(130, 0), new Color(0.7f, 0.15f, 0.15f, 0.95f), () => {
                IsAccepted = false;
                _panel.SetActive(false);
                Variables.menuTrigger = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Utility.ForceMessage("<color=#FF3333>You declined the agreement. BOT Menu is disabled.</color>");
            });

            _panel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string name, Vector2 pos, Color btnCol, Action onClick)
        {
            GameObject bObj = new GameObject(name);
            bObj.transform.SetParent(parent, false);
            RectTransform rt = bObj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(210, 38);

            Image img = bObj.AddComponent<Image>();
            img.color = btnCol;

            Button btn = bObj.AddComponent<Button>();
            btn.onClick.AddListener((UnityEngine.Events.UnityAction)onClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = 14;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return btn;
        }

        public void Show()
        {
            UpdateLanguageText();
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateLanguageText()
        {
            _titleText.text = MenuUtility.T(
                "✦ TERMS OF USE & DISCLAIMER ✦",
                "✦ 用户使用协议与免责声明 ✦",
                "✦ ПОЛЬЗОВАТЕЛЬСКОЕ СОГЛАШЕНИЕ ✦",
                "✦ KULLANIM KOŞULLARI VE SORUMLULUK REDDİ ✦",
                "✦ TERMOS DE USO E ISENÇÃO DE RESPONSABILIDADE ✦"
            );

            _bodyText.text = MenuUtility.T(

                "<b>1. Educational & Research Purpose:</b>\nThis mod menu is developed strictly for learning Unity IL2CPP game mechanics, network protocol analysis, and educational safety testing.\n\n" +
                "<b>2. User Responsibility & Ban Risk:</b>\nUsing modifications in multiplayer games may violate game Terms of Service. You assume all responsibility for account penalties, bans, or blacklists.\n\n" +
                "<b>3. Fair Play & Server Respect:</b>\nDo not use malicious crash features to ruin public multiplayer gameplay or harass others.\n\n" +
                "<b>4. Voluntary Agreement:</b>\nBy clicking <b>'AGREE'</b>, you acknowledge that you have read, understood, and accepted all terms above. If you click <b>'DECLINE'</b>, this mod will remain completely disabled.",

                "<b>1. 仅供学习与安全研究：</b>\n本模组仅供 Unity IL2CPP 逆向工程、网络封包结构分析与个人技术交流测试，严禁用于任何商业或非法牟利行为。\n\n" +
                "<b>2. 封禁风险与责任自负：</b>\n在多人联机游戏中使用辅助模组可能存在被游戏封禁、列入服务器黑名单的风险。使用者须自行承担所有后果与责任。\n\n" +
                "<b>3. 规范使用与尊重环境：</b>\n请勿在公共绿色房间恶意炸房、恶意卡服或恶意骚扰正常玩家。\n\n" +
                "<b>4. 自愿授权协议：</b>\n点击<b>【同意并继续】</b>即代表您已完整阅读并自愿接受本声明的所有条款。若点击<b>【拒绝】</b>，模组将立即永久停用。",

                "<b>1. В образовательных целях:</b>\nДанный мод разработан исключительно для изучения Unity IL2CPP и анализа сетевых протоколов.\n\n" +
                "<b>2. Ответственность и риски:</b>\nИспользование модификаций может привести к блокировке аккаунта. Вы несете полную ответственность за любые последствия.\n\n" +
                "<b>3. Согласие:</b>\nНажимая <b>'ПРИНЯТЬ'</b>, вы подтверждаете согласие с правилами. При нажатии <b>'ОТКЛОНИТЬ'</b> мод будет выключен.",

                "<b>1. Eğitim ve Araştırma Amaçlı:</b>\nBu mod menüsü yalnızca Unity IL2CPP mekaniklerini ve ağ protokollerini test etmek amacıyla geliştirilmiştir.\n\n" +
                "<b>2. Kullanıcı Sorumluluğu:</b>\nMod kullanımı hesap yasaklamalarına yol açabilir. Tüm sorumluluk kullanıcıya aittir.\n\n" +
                "<b>3. Kabul:</b>\n<b>'KABUL ET'</b> butonuna basarak tüm şartları onaylamış olursunuz. <b>'REDDET'</b> seçilirse mod devre dışı kalacaktır.",

                "<b>1. Fins Educacionais:</b>\nEste mod foi desenvolvido estritamente para estudo de engenharia reversa no Unity IL2CPP e testes de rede.\n\n" +
                "<b>2. Risco e Responsabilidade:</b>\nO uso de modificações pode resultar em banimento. Você assume total responsabilidade pelo uso.\n\n" +
                "<b>3. Acordo:</b>\nAo clicar em <b>'CONCORDAR'</b>, você aceita todos os termos. Ao clicar em <b>'RECUSAR'</b>, o mod permanecerá desativado."
            );

            _agreeBtn.GetComponentInChildren<Text>().text = MenuUtility.T(
                "AGREE & CONTINUE",
                "同意并继续 (AGREE)",
                "ПРИНЯТЬ",
                "KABUL ET",
                "CONCORDAR E CONTINUAR"
            );

            _declineBtn.GetComponentInChildren<Text>().text = MenuUtility.T(
                "DECLINE & EXIT",
                "拒绝并停用 (DECLINE)",
                "ОТКЛОНИТЬ",
                "REDDET VE ÇIK",
                "RECUSAR E SAIR"
            );
        }
    }
}
