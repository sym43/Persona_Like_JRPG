using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 피해 속성별 저항을 묶은 데이터. <br/>
/// 스킬 계산과 도감 표시에도 함께 사용함.
/// </summary>
public sealed class ResistanceTable
{
    //속성별 저항 목록
    public IReadOnlyDictionary<DamageType, ResistanceType> Entries { get; }

    //저항 표를 만듦
    public ResistanceTable(IEnumerable<KeyValuePair<DamageType, ResistanceType>> entries)
    {
        #region 입력값 검사

        if (entries == null) throw new ArgumentNullException(nameof(entries), "저항 목록이 필요합니다.");
        var copy = new Dictionary<DamageType, ResistanceType>();
        foreach (var entry in entries)
        {
            if (!Enum.IsDefined(typeof(DamageType), entry.Key))
                throw new ArgumentException("알 수 없는 피해 속성입니다.", nameof(entries));
            if (!Enum.IsDefined(typeof(ResistanceType), entry.Value))
                throw new ArgumentException("알 수 없는 내성 종류입니다.", nameof(entries));
            if (copy.ContainsKey(entry.Key))
                throw new ArgumentException("중복된 피해 속성이 있습니다.", nameof(entries));
            copy.Add(entry.Key, entry.Value);
        }

        var expectedCount = Enum.GetValues(typeof(DamageType)).Length;
        if (copy.Count != expectedCount)
            throw new ArgumentException("모든 피해 속성의 내성 정보가 필요합니다.", nameof(entries));

        #endregion

        Entries = new ReadOnlyDictionary<DamageType, ResistanceType>(copy);
    }

    //특정 속성의 저항을 가져옴
    public ResistanceType Get(DamageType damageType)
    {
        #region 입력값 검사

        if (!Enum.IsDefined(typeof(DamageType), damageType))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");

        #endregion

        return Entries[damageType];
    }
}
