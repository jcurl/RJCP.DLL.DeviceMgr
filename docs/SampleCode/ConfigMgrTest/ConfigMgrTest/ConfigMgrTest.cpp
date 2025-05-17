#include <Windows.h>
#include <cfgmgr32.h>

#include <iostream>

#include "cfgmgr32_list.h"
#include "cfgmgr32_child.h"

enum class arg_mode {
    unknown,
    list,
    recurse
};

int main(int argc, char **argv) {
    if (argc > 2) {
        std::wcerr << L"Error in arguments" << std::endl;
        return 1;
    }

    arg_mode mode = arg_mode::unknown;
    if (argc == 2) {
        if (strcmp("recurse", argv[1]) == 0) {
            mode = arg_mode::recurse;
        }
        else if (strcmp("list", argv[1]) == 0) {
            mode = arg_mode::list;
        }
        if (mode == arg_mode::unknown) {
            std::wcerr << L"Mode should be in [recurse, list]." << std::endl;
            return 1;
        }
    } else {
        mode = arg_mode::list;
    }

    switch (mode) {
    case arg_mode::list:
        cfgmgr32_list();
        break;
    case arg_mode::recurse:
        cfgmgr32_child_recurse();
        break;
    default:
        std::wcerr << L"Internal error, unknown mode " << (int)mode << std::endl;
    }

    return 0;
}
