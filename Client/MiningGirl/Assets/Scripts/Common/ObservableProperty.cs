using System;
using System.Collections.Generic;

// 값이 실제로 바뀔 때만 알리는 프로퍼티.
//
// UniRx도 R3도 프로젝트에 없어서, 필요한 만큼만 직접 두었습니다.
// 매 프레임 같은 값을 넣어도 구독자에게는 바뀐 순간만 갑니다.
// 예를 들어 "03:24" 같은 시간 문자열은 프레임마다 넣어도 알림은 초당 한 번입니다.
public class ObservableProperty<T>
{
    public event Action<T> Changed;

    private T _value;

    public ObservableProperty(T initialValue = default)
    {
        _value = initialValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;

            Changed?.Invoke(_value);
        }
    }

    // 구독하면서 현재 값을 한 번 흘려 줍니다.
    // 이게 없으면 View가 첫 변화가 올 때까지 빈 채로 있습니다.
    public void Bind(Action<T> handler)
    {
        if (handler == null)
            return;

        Changed += handler;

        handler(_value);
    }

    public void Unbind(Action<T> handler)
    {
        if (handler != null)
            Changed -= handler;
    }

    // View가 사라질 때 남은 구독을 한 번에 끊습니다.
    public void ClearSubscribers()
    {
        Changed = null;
    }
}
