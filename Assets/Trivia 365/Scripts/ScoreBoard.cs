using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ScoreBoard : MonoBehaviour
{
    public class Record
    {
        public string uuid { get; set; }
        public string player { get; set; }
        public string score { get; set; }
        public string ptime { get; set; }
    }

    public class Records
    {
        public List<Record> records { get; set; }
    }

    public List<Record> items;
    /// <summary>
    /// ����������
    /// </summary>
    public int ItemsCount;
    /// <summary>
    /// ��ҳ����û������Ĭ��Ϊ1
    /// </summary>
    public int PageCount = 1;
    /// <summary>
    /// ��ǰҳ���ı�ǩ
    /// </summary>
    public Text PanelText;
    /// <summary>
    /// ��ǰҳ������
    /// </summary>
    public int PageIndex = 1;
    /// <summary>
    /// ��һҳ��ť
    /// </summary>
    public Button BtnPrevious;
    /// <summary>
    /// ��һҳ��ť
    /// </summary>
    public Button BtnNext;
    /// <summary>
    /// ��������������е�������ȫ�����������
    /// </summary>
    public GameObject ParentObj;
    /// <summary>
    /// ��Ҫʹ�õ�Ԥ�Ƽ�,����Ϸ�����ϻ�󶨸����¼�����Դ�ȣ�ͬʱҲ��������
    /// </summary>
    public GameObject gameObjectPrefab;
    void Start()
    {
        InitUGUI();
        //Init();
    }

    /// <summary>
    /// ��ʼ��UGUI
    /// </summary>
    private void InitUGUI()
    {
        DestroyChildObject(ParentObj);
    }

    /// <summary>
    /// ��һҳ�¼�
    /// </summary>
    public void Next()
    {
        //���һҳ��ֹ��ҳ
        if (PageIndex == PageCount)
            return;
        if (PageIndex >= PageCount)
            PageIndex = PageCount;
        DestroyChildObject(ParentObj);
        PageIndex += 1;
        UpdateUI(PageIndex);
        //����ҳ��ҳ��
        PanelText.text = string.Format("{0}/{1}", PageIndex.ToString(), PageCount.ToString());
    }

    public void Previous()
    {
        //��һҳ��ֹ��ҳ
        if (PageIndex == 1)
            return;
        DestroyChildObject(ParentObj);
        PageIndex -= 1;
        UpdateUI(PageIndex);
        //����ҳ��ҳ��
        PanelText.text = string.Format("{0}/{1}", PageIndex.ToString(), PageCount.ToString());
    }

    /// <summary>
    /// ��ʼ��Ԫ��
    /// </summary>
    public void Init(List<Record> records)
    {
        //����Ԫ���ܸ���        
        items = records;

        ItemsCount = items.Count;
        //������ҳ��
        PageCount = items.Count % 10 == 0 ? items.Count / 10 : items.Count / 10 + 1;
        if (items.Count <= 10)
            PageCount = 1;
        //���ð�ҳ������
        UpdateUI(PageIndex);
        //���½���ҳ��
        PanelText.text = string.Format("{0}/{1}", PageIndex.ToString(), PageCount.ToString());
    }
    /// <summary>
    /// ��ҳ������
    /// </summary>
    /// <param name="��ǰҳ��"></param>
    public void UpdateUI(int currentIndex)
    {
        //û��������ֱ��return
        if (ItemsCount <= 0)
        {
            return;
        }
        for (int i = (PageIndex - 1) * 10; i < ((PageIndex - 1) * 10 + 10 > ItemsCount ? ItemsCount : (PageIndex - 1) * 10 + 10); i++)
        {
            var record = Instantiate(gameObjectPrefab) as GameObject;
            record.transform.SetParent(ParentObj.transform,false);
            // rank
            int rank = (i + 1);
            record.transform.GetChild(0).GetComponent<Text>().text = string.Concat(rank.ToString());
            // name
            record.transform.GetChild(1).GetComponent<Text>().text = string.Concat(items[i].player);
            // score
            record.transform.GetChild(2).GetComponent<Text>().text = string.Concat(items[i].score.ToString());
        }
    }

    /// <summary>
    /// ɾ�������µ��Ӷ���
    /// </summary>
    /// <param name="������"></param>
    public void DestroyChildObject(GameObject parentObject)
    {
        if (parentObject == null)
            return;
        for (int i = parentObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(parentObject.transform.GetChild(i).gameObject);
        }
        Resources.UnloadUnusedAssets();
    }

    public void getData(string jsonstr)
    {
        // �������
        Records allRecords = JsonConvert.DeserializeObject<Records>(jsonstr);
        //Debug.Log(allRecords.records[0].uuid);
        //Debug.Log(allRecords.records[0].player);
        //Debug.Log(allRecords.records[0].score);
        //Debug.Log(allRecords.records[0].ptime);
        InitUGUI();
        Init(allRecords.records);
    }



   

}
