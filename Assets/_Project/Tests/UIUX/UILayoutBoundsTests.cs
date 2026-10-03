using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.UIUX
{
    [TestFixture]
    public class UILayoutBoundsTests
    {
        private GameObject _holder;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("LayoutTestHolder");
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(720, 1280)]
        public void WeaponEdit_Header_Buttons_And_Title_Do_Not_Overlap(int screenWidth, int screenHeight)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(_holder.transform);
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(screenWidth, screenHeight);

            // Header (height 70, stretched top)
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(canvasGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(screenWidth, 70f);

            // Back button on left
            var backBtn = new GameObject("BackButton", typeof(RectTransform));
            backBtn.transform.SetParent(headerGo.transform, false);
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0.5f);
            backRt.anchorMax = new Vector2(0f, 0.5f);
            backRt.anchoredPosition = new Vector2(15f, 0f);
            backRt.sizeDelta = new Vector2(100f, 40f);

            // Title flexible stretched in center
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(120f, 0f);
            titleRt.offsetMax = new Vector2(-215f, 0f);

            // Explode and Reset buttons on right
            var explodeBtn = new GameObject("ExplodeButton", typeof(RectTransform));
            explodeBtn.transform.SetParent(headerGo.transform, false);
            var explodeRt = explodeBtn.GetComponent<RectTransform>();
            explodeRt.anchorMin = new Vector2(1f, 0.5f);
            explodeRt.anchorMax = new Vector2(1f, 0.5f);
            explodeRt.anchoredPosition = new Vector2(-115f, 0f);
            explodeRt.sizeDelta = new Vector2(95f, 40f);

            var resetBtn = new GameObject("ResetButton", typeof(RectTransform));
            resetBtn.transform.SetParent(headerGo.transform, false);
            var resetRt = resetBtn.GetComponent<RectTransform>();
            resetRt.anchorMin = new Vector2(1f, 0.5f);
            resetRt.anchorMax = new Vector2(1f, 0.5f);
            resetRt.anchoredPosition = new Vector2(-15f, 0f);
            resetRt.sizeDelta = new Vector2(90f, 40f);

            Canvas.ForceUpdateCanvases();

            Vector3[] backCorners = new Vector3[4];
            Vector3[] titleCorners = new Vector3[4];
            Vector3[] explodeCorners = new Vector3[4];
            Vector3[] resetCorners = new Vector3[4];

            backRt.GetWorldCorners(backCorners);
            titleRt.GetWorldCorners(titleCorners);
            explodeRt.GetWorldCorners(explodeCorners);
            resetRt.GetWorldCorners(resetCorners);

            // Check horizontal clearances
            Assert.LessOrEqual(backCorners[2].x, titleCorners[0].x,
                $"BackButton right edge ({backCorners[2].x}) must not exceed Title left edge ({titleCorners[0].x}) at {screenWidth}x{screenHeight}");

            Assert.LessOrEqual(titleCorners[2].x, explodeCorners[0].x,
                $"Title right edge ({titleCorners[2].x}) must not exceed ExplodeButton left edge ({explodeCorners[0].x}) at {screenWidth}x{screenHeight}");

            Assert.LessOrEqual(explodeCorners[2].x, resetCorners[0].x,
                $"ExplodeButton right edge ({explodeCorners[2].x}) must not exceed ResetButton left edge ({resetCorners[0].x}) at {screenWidth}x{screenHeight}");
        }

        [Test]
        public void WeaponEdit_BottomCard_Internal_Rows_Do_Not_Overlap()
        {
            var cardGo = new GameObject("BottomCard", typeof(RectTransform));
            cardGo.transform.SetParent(_holder.transform);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(1280f, 215f);

            // Slot label at top row (y = 185, height = 26)
            var labelGo = new GameObject("SlotLabel", typeof(RectTransform));
            labelGo.transform.SetParent(cardGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0f, 0f);
            labelRt.anchoredPosition = new Vector2(25f, 185f);
            labelRt.sizeDelta = new Vector2(200f, 26f);

            // Parts scroll view at middle row (y = 105, height = 72)
            var partsGo = new GameObject("PartsScrollView", typeof(RectTransform));
            partsGo.transform.SetParent(cardGo.transform, false);
            var partsRt = partsGo.GetComponent<RectTransform>();
            partsRt.anchorMin = new Vector2(0f, 0f);
            partsRt.anchorMax = new Vector2(0.68f, 0f);
            partsRt.pivot = new Vector2(0.5f, 0.5f);
            partsRt.anchoredPosition = new Vector2(20f, 105f);
            partsRt.sizeDelta = new Vector2(-30f, 72f);

            // Action buttons at bottom row (y = 14, height = 42, pivot = 0,0)
            var actionGo = new GameObject("ActionButton", typeof(RectTransform));
            actionGo.transform.SetParent(cardGo.transform, false);
            var actionRt = actionGo.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(0f, 0f);
            actionRt.anchorMax = new Vector2(0f, 0f);
            actionRt.pivot = new Vector2(0f, 0f);
            actionRt.anchoredPosition = new Vector2(20f, 14f);
            actionRt.sizeDelta = new Vector2(150f, 42f);

            Canvas.ForceUpdateCanvases();

            Vector3[] labelCorners = new Vector3[4];
            Vector3[] partsCorners = new Vector3[4];
            Vector3[] actionCorners = new Vector3[4];

            labelRt.GetWorldCorners(labelCorners);
            partsRt.GetWorldCorners(partsCorners);
            actionRt.GetWorldCorners(actionCorners);

            // Vertical separation: Action buttons < Parts < Label
            Assert.Less(actionCorners[1].y, partsCorners[0].y,
                $"Action buttons top edge ({actionCorners[1].y}) must be below Parts Scroll View bottom edge ({partsCorners[0].y})");

            Assert.Less(partsCorners[1].y, labelCorners[0].y,
                $"Parts Scroll View top edge ({partsCorners[1].y}) must be below Slot Label bottom edge ({labelCorners[0].y})");
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(720, 1280)]
        public void Lobby_TopBar_Profile_And_Stats_Do_Not_Overlap(int screenWidth, int screenHeight)
        {
            var topBarGo = new GameObject("TopBar", typeof(RectTransform));
            topBarGo.transform.SetParent(_holder.transform);
            var barRt = topBarGo.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(screenWidth, 75f);

            // Profile Box (Left)
            var profileGo = new GameObject("ProfileBox", typeof(RectTransform));
            profileGo.transform.SetParent(topBarGo.transform, false);
            var profileRt = profileGo.GetComponent<RectTransform>();
            profileRt.anchorMin = new Vector2(0f, 0.5f);
            profileRt.anchorMax = new Vector2(0f, 0.5f);
            profileRt.anchoredPosition = new Vector2(25f, 0f);
            profileRt.sizeDelta = new Vector2(250f, 60f);

            // Stats Box (Right)
            var statsGo = new GameObject("StatsBox", typeof(RectTransform));
            statsGo.transform.SetParent(topBarGo.transform, false);
            var statsRt = statsGo.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(1f, 0.5f);
            statsRt.anchorMax = new Vector2(1f, 0.5f);
            statsRt.anchoredPosition = new Vector2(-90f, 0f);
            statsRt.sizeDelta = new Vector2(260f, 60f);

            Canvas.ForceUpdateCanvases();

            Vector3[] profCorners = new Vector3[4];
            Vector3[] statsCorners = new Vector3[4];

            profileRt.GetWorldCorners(profCorners);
            statsRt.GetWorldCorners(statsCorners);

            Assert.LessOrEqual(profCorners[2].x, statsCorners[0].x,
                $"Profile right edge ({profCorners[2].x}) must not overlap Stats left edge ({statsCorners[0].x}) at {screenWidth}x{screenHeight}");
        }
    }
}
