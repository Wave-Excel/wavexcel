namespace Models.AdditionalLoadPointModel;
public class AdditionalLoadPoint
{

    private List<CustomerLoadPoint> customerLoadPoint = new List<CustomerLoadPoint>();

    public static AdditionalLoadPoint addionalLoadPoint;
    private int _k;

    public int K
    {
        get { return _k; }
        set { _k = value; }
    }


    public void FillCustomerLoadPoint()
    {
        for (int i = 0; i <= this._k; ++i)
        {
            customerLoadPoint.Add(new CustomerLoadPoint());
        }
    }


    private AdditionalLoadPoint()
    {

    }
    public static AdditionalLoadPoint GetInstance()
    {
        if (addionalLoadPoint == null)
        {
            addionalLoadPoint = new AdditionalLoadPoint();
        }
        return addionalLoadPoint;
    }
    public List<CustomerLoadPoint> CustomerLoadPoints
    {
        get { return customerLoadPoint; }
        set
        {
            customerLoadPoint = value;
        }
    }
    public void SortCustomerLoadPointsByPower()
    {
        // customerLoadPoint.Sort((x, y) => y.PowerGeneration.CompareTo(x.PowerGeneration));

        var sublist = customerLoadPoint.GetRange(1, customerLoadPoint.Count - 1);
        sublist.Sort((x, y) => y.PowerGeneration.CompareTo(x.PowerGeneration));

        // Insert the sorted sublist back into the original list
        for (int i = 1; i < customerLoadPoint.Count; i++)
        {
            customerLoadPoint[i] = sublist[i - 1];
        }
    }

    public void SortCustomerLoadPointsByVol()
    {
        var sublist = customerLoadPoint.GetRange(1, customerLoadPoint.Count - 1);
        sublist.Sort((x, y) => y.VolFlow.CompareTo(x.VolFlow));

        // Insert the sorted sublist back into the original list
        for (int i = 1; i < customerLoadPoint.Count; i++)
        {
            customerLoadPoint[i] = sublist[i - 1];
        }
        // customerLoadPoint.Sort((x, y) => y.VolFlow.CompareTo(x.VolFlow));
    }
}
public class CustomerLoadPoint
{
    private int _lpNumber;
    private double _steamPressure;
    private double _steamTemp;
    private double _steamMass;
    private double _exhaustPressure;
    private double _powerGeneration;
    private double _exhasutMassFlow;
    private double _partLoad;
    private double _volFlow;
    private double _effFromTurba;
    private string _loadPoint;

    private double _pst;// = 150;

    private double _makeupTemp;// = 35;

    private double _condretTemp;

    private double _processcondreturn;

    private double _deaeratoroutlettemp;

    private double _capacity;



    public double Capacity
    {
        get
        {
            return _capacity;
        }
        set
        {
            _capacity = value;
        }
    }

    public double DeaeratorOutletTemp
    {
        get
        {
            return _deaeratoroutlettemp;
        }
        set
        {
            _deaeratoroutlettemp = value;
        }
    }
    public double ProcessCondReturn
    {
        get
        {
            return _processcondreturn;
        }
        set
        {
            _processcondreturn = value;
        }
    }
    public double CondRetTemp
    {
        get
        {
            return _condretTemp;
        }
        set
        {
            _condretTemp = value;
        }
    }


    public double MakeUpTempe
    {
        get
        {
            return _makeupTemp;
        }
        set
        {
            _makeupTemp = value;
        }
    }
    public double ExhaustMassFlow
    {
        get { return _exhasutMassFlow; }
        set { _exhasutMassFlow = value; }
    }
    public double PST
    {
        get { return _pst; }
        set { _pst= value; }
    }
    public string LoadPoint
    {
        get { return _loadPoint; }
        set { _loadPoint = value; }
    }
    public double VolFlow
    {
        get { return _volFlow; }
        set { _volFlow = value; }
    }
    public double EffFromTurba
    {
        get { return _effFromTurba; }
        set
        {
            _effFromTurba = value;
        }
    }
    public double PartLoad
    {
        get { return _partLoad; }
        set { _partLoad = value; }
    }
    public int LPNumber
    {
        get { return _lpNumber; }
        set { _lpNumber = value; }
    }
    public double SteamPressure
    {
        get { return _steamPressure; }
        set { _steamPressure = value; }
    }
    public double SteamTemp
    {
        get { return _steamTemp; }
        set { _steamTemp = value; }
    }
    public double SteamMass
    {
        get { return _steamMass; }
        set { _steamMass = value; }
    }
    public double ExhaustPressure
    {
        get { return _exhaustPressure; }
        set { _exhaustPressure = value; }
    }
    public double PowerGeneration
    {
        get { return _powerGeneration; }
        set { _powerGeneration = value; }
    }

    // only applicable for UI fields while counting valid parameters!
    public int CountValidParameters()
    {
        int count = 0;

        if (PowerGeneration != 0) count++;
        if (ExhaustPressure != 0) count++;
        if (SteamMass != 0) count++;
        if (SteamTemp != 0) count++;
        if (SteamPressure != 0) count++;
        if (ExhaustMassFlow != 0) count++;
        if (PartLoad != 0) count++;

        return count;
    }
}