#pragma once

#include "App.g.h"
#include "App.base.h"

namespace winrt::IslandsNET6::Components::implementation
{
    struct App : AppT2<App>
    {
        App();
    };
}

namespace winrt::IslandsNET6::Components::factory_implementation
{
    struct App : AppT<App, implementation::App>
    {
    };
}
