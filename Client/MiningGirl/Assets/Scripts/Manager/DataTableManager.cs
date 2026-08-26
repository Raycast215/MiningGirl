using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;

namespace Manager
{
    public class DataTableManager : SingletonBase<DataTableManager>
    {
        // 삭제 예정
        public CharacterStatDataTable CharacterStatDataTable { get; private set; }
        public CharacterStatGrowthDataTable CharacterStatGrowthDataTable { get; private set; }
        public LevelUpBonusSkillDataTable LevelUpBonusSkillDataTable { get; private set; }
        public SkillCardDataTable SkillCardDataTable { get; private set; }
        public DefaultSkillCardDataTable DefaultSkillCardDataTable { get; private set; }
        public GameConstantDataTable GameConstantDataTable { get; private set; }
        //
        
        public StageDataTable StageDataTable { get; private set; }

        public async UniTaskVoid PreLoadData()
        {
            if (IsInitialized)
                return;

            bool success = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");

            if (!success)
            {
                Debug.LogError("Data load failed");
                return;
            }

            // 삭제 예정
            CharacterStatDataTable = Create<CharacterStatDataTable>();
            CharacterStatGrowthDataTable = Create<CharacterStatGrowthDataTable>();
            LevelUpBonusSkillDataTable = Create<LevelUpBonusSkillDataTable>();
            SkillCardDataTable = Create<SkillCardDataTable>();
            DefaultSkillCardDataTable = Create<DefaultSkillCardDataTable>();
            GameConstantDataTable = Create<GameConstantDataTable>();
            // 
            
            StageDataTable = Create<StageDataTable>();

            IsInitialized = true;
        }

        // 테이블 타입만 주면 행 타입은 알아서 찾아 만듭니다.
        //
        // 모든 테이블이 DataTableBase<TRow>를 상속하므로, 부모 제네릭 인자에서
        // 행 타입을 꺼낼 수 있습니다. 덕분에 호출부에서 행 타입을 또 적지 않아도 됩니다.
        // (전에는 new SkillCardDataTable(GetAll<SkillCardDataTableRow>()) 처럼 두 번 적었습니다.)
        private static TTable Create<TTable>() where TTable : class
        {
            var rowType = FindRowType(typeof(TTable));

            if (rowType == null)
            {
                Debug.LogError($"[DataTable] {typeof(TTable).Name}의 행 타입을 찾지 못했습니다.");

                return null;
            }

            // GetAll<TRow>()를 리플렉션으로 호출합니다.
            var getAll = typeof(AddressableSheetsDataManager)
                .GetMethod(nameof(AddressableSheetsDataManager.GetAll), BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(rowType);

            if (getAll == null)
            {
                Debug.LogError("[DataTable] GetAll 메서드를 찾지 못했습니다.");

                return null;
            }

            var rows = getAll.Invoke(null, null);

            return Activator.CreateInstance(typeof(TTable), rows) as TTable;
        }

        // 상속 사슬을 거슬러 올라가며 DataTableBase<>의 제네릭 인자를 찾습니다.
        private static Type FindRowType(Type tableType)
        {
            for (var type = tableType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(DataTableBase<>))
                    continue;

                return type.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }
    }
}
