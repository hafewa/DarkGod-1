/****************************************************
    文件：MainCitySys.cs
	作者：朱龙飞
    邮箱: 398670608@qq.com
    日期：2019/7/13 14:57:17
	功能：主城业务系统
*****************************************************/

using PEProtocol;
using UnityEngine;
using UnityEngine.AI;

public class MainCitySys : SystemRoot {
    public static MainCitySys Instance = null;

    public MainCityWnd mainCityWnd;
    public InfoWnd infoWnd;
    public GuideWnd guideWnd;
    public StrongWnd strongWnd;
    public ChatWnd chatWnd;
    public BuyWnd buyWnd;
    public TaskWnd taskWnd;

    private PlayerController playerCtrl;
    private Transform charCamTrans;
    private AutoGuideCfg curtTaskData;
    private Transform[] npcPosTrans;
    private NavMeshAgent nav;

    public override void InitSys() {
        base.InitSys();

        Instance = this;
        PECommon.Log("Init MainCitySys...");
    }

    public void EnterMainCity() {
        MapCfg mapData = resSvc.GetMapCfgData(Constants.MainCityMapID);

        resSvc.AsyncLoadScene(mapData.sceneName, () =>
        {
            PECommon.Log("Enter MainCity...");

            //TODO 加载游戏主角
            LoadPlayer(mapData);

            //打开主城场景UI
            mainCityWnd.SetWndState(true);

            GameRoot.Instance.GetComponent<AudioListener>().enabled = false;
            //播放主城背景音乐
            audioSvc.PlayBGMusic(Constants.BGMainCity);

            GameObject map = GameObject.FindGameObjectWithTag("MapRoot");
            MainCityMap mcm = map.GetComponent<MainCityMap>();
            npcPosTrans = mcm.NpcPosTrans;

            //设置人物展示
            if (charCamTrans == null) {
                charCamTrans = GameObject.FindGameObjectWithTag("CharShowCam").transform;
            }
            charCamTrans.gameObject.SetActive(false);
        });
    }

    private void LoadPlayer(MapCfg mapData) {
        GameObject player = resSvc.LoadPrefab(PathDefine.AssassinCityPlayerPrefab, true);

        player.transform.position = mapData.playerBornPos;
        player.transform.localEulerAngles = mapData.playerBornRote;
        player.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        //相机初始化
        Camera.main.transform.position = mapData.mainCamPos;
        Camera.main.transform.localEulerAngles = mapData.mainCamRote;

        playerCtrl = player.GetComponent<PlayerController>();
        playerCtrl.Init();
        nav = player.GetComponent<NavMeshAgent>();

    }

    public void SetMoveDir(Vector2 dir) {
        StopNavTask();
        if (dir == Vector2.zero) {
            playerCtrl.SetBlend(Constants.BlendIdle);
        }
        else {
            playerCtrl.SetBlend(Constants.BlendMove);
        }

        playerCtrl.Dir = dir;
    }

    #region Enter FubenSys
    public void EnterFuben() {
        StopNavTask();
        FubenSys.Instance.EnterFuben();
    }
    #endregion

    #region Task Wnd
    public void OpenTaskRewardWnd() {
        StopNavTask();
        taskWnd.SetWndState(true);
    }

    public void RspTakeTaskReward(GameMsg msg) {
        RspTakeTaskReward data = msg.rspTakeTaskReward;
        GameRoot.Instance.SetPlayerDataByTask(data);

        taskWnd.RefreshUI();
        mainCityWnd.RefreshUI();
    }

    public void PshTaskPrgs(GameMsg msg) {
        PshTaskPrgs data = msg.pshTaskPrgs;
        GameRoot.Instance.SetPlayerDataByTaskPsh(data);

        if (taskWnd.GetWndSatate()) {
            taskWnd.RefreshUI();
        }
    }
    #endregion

    #region Buy Wnd
    public void OpenBuyWnd(int type) {
        StopNavTask();
        buyWnd.SetBuyType(type);
        buyWnd.SetWndState(true);
    }

    public void RspBuy(GameMsg msg) {
        RspBuy rspBuy = msg.rspBuy;
        GameRoot.Instance.SetPlayerDataByBuy(rspBuy);

        GameRoot.AddTips("购买成功");

        mainCityWnd.RefreshUI();
        buyWnd.SetWndState(false);

        if (msg.pshTaskPrgs != null) {
            PshTaskPrgs(msg);
        }
    }

    public void PshPower(GameMsg msg) {
        PshPower data = msg.pshPower;
        GameRoot.Instance.SetPlayerDataByPower(data);
        if (mainCityWnd.GetWndSatate()) {
            mainCityWnd.RefreshUI();
        }
    }
    #endregion

    #region Chat Wnd
    public void OpenChatWnd() {
        StopNavTask();
        chatWnd.SetWndState(true);
    }

    public void PshCaht(GameMsg msg) {
        chatWnd.AddChatMsg(msg.pshChat.name, msg.pshChat.chat);

        if (msg.pshTaskPrgs != null) {
            PshTaskPrgs(msg);
        }
    }
    #endregion

    #region Strong Wnd
    public void OpenStrongWnd() {
        StopNavTask();
        strongWnd.SetWndState(true);
    }

    public void RspStrong(GameMsg msg) {
        int zhanliPre = PECommon.GetFightByProps(GameRoot.Instance.PlayerData);
        GameRoot.Instance.SetPlayerDataByRspStrong(msg.rspStrong);
        int zhanliNow = PECommon.GetFightByProps(GameRoot.Instance.PlayerData);
        GameRoot.AddTips(Constants.Color("战力提升" + (zhanliNow - zhanliPre), TxtColor.Blue));

        strongWnd.UpdateUI();
        mainCityWnd.RefreshUI();

        if (msg.pshTaskPrgs != null) {
            PshTaskPrgs(msg);
        }
    }
    #endregion

    #region Info Wnd
    public void OpenInfoWnd() {
        StopNavTask();

        //设置人物展示相机的相对位置
        charCamTrans.localPosition = playerCtrl.transform.position + playerCtrl.transform.forward * 3.8f + new Vector3(0, 1.2f, 0);
        charCamTrans.localEulerAngles = new Vector3(0, 180f + playerCtrl.transform.localEulerAngles.y, 0);
        charCamTrans.localScale = Vector3.one;
        charCamTrans.gameObject.SetActive(true);

        infoWnd.SetWndState(true);
    }

    public void CloseInfoWnd() {
        if (charCamTrans != null) {
            charCamTrans.gameObject.SetActive(false);
        }
        infoWnd.SetWndState(false);
    }

    private float startRoate = 0;
    public void SetStartRoate() {
        startRoate = playerCtrl.transform.localEulerAngles.y;
    }

    public void SetPlayerRoate(float roate) {
        playerCtrl.transform.localEulerAngles = new Vector3(0, startRoate + roate, 0);
    }
    #endregion

    #region Guide Wnd
    private bool isNavGuide = false;
    public void RunTask(AutoGuideCfg agc) {
        if (agc != null) {
            curtTaskData = agc;
        }

        //解析任务数据
        nav.enabled = true;
        if (curtTaskData.npcID != -1) {
            Vector3 dest = npcPosTrans[agc.npcID].position;
            float dis = Vector3.Distance(playerCtrl.transform.position, dest);
            if (dis < 0.5f) {
                isNavGuide = false;
                nav.isStopped = true;
                nav.enabled = false;
                playerCtrl.SetBlend(Constants.BlendIdle);

                OpenGuideWnd();
            }
            else {
                isNavGuide = true;
                nav.enabled = true;
                nav.speed = Constants.PlayerMoveSpeed;
                nav.SetDestination(dest);
                playerCtrl.SetBlend(Constants.BlendMove);
            }
        }
        else {
            OpenGuideWnd();
        }
    }

    private void IsArriveNavPos() {
        Vector3 dest = npcPosTrans[curtTaskData.npcID].position;
        float dis = Vector3.Distance(playerCtrl.transform.position, dest);
        if (dis < 0.5f) {
            isNavGuide = false;
            nav.isStopped = true;
            nav.enabled = false;
            playerCtrl.SetBlend(Constants.BlendIdle);

            OpenGuideWnd();
        }
    }

    private void Update() {
        if (isNavGuide) {
            IsArriveNavPos();
            playerCtrl.SetCam();
        }
    }

    private void StopNavTask() {
        if (isNavGuide) {
            isNavGuide = false;
            nav.isStopped = true;
            nav.enabled = false;
            playerCtrl.SetBlend(Constants.BlendIdle);
        }
    }

    private void OpenGuideWnd() {
        guideWnd.SetWndState(true);
    }

    public AutoGuideCfg GetCurtTaskData() {
        return curtTaskData;
    }

    public void RspGuide(GameMsg msg) {
        RspGuide data = msg.rspGuide;

        GameRoot.AddTips(Constants.Color("任务奖励 金币+" + curtTaskData.coin + " 经验+" + curtTaskData.exp, TxtColor.Blue));

        switch (curtTaskData.actID) {
            case 0:
                //于智者对话
                break;
            case 1:
                //TODO 进入副本
                EnterFuben();
                break;
            case 2:
                //TODO 进入强化界面
                OpenStrongWnd();
                break;
            case 3:
                //TODO 进入体力购买
                OpenBuyWnd(0);
                break;
            case 4:
                //TODO 进入金币铸造
                OpenBuyWnd(1);
                break;
            case 5:
                //TODO 进入世界聊天
                OpenChatWnd();
                break;
            default:
                break;
        }
        GameRoot.Instance.SetPlayerDataByGuide(data);
        mainCityWnd.RefreshUI();

        if (msg.pshTaskPrgs != null) {
            PshTaskPrgs(msg);
        }
    }
    #endregion

}