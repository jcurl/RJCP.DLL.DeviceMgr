#include <Windows.h>
#include <cfgmgr32.h>
#include <iostream>
#include <string>

#include "cfgmgr32_child.h"
#include "cfgmgr32_common.h"

namespace {
    void recurse(DEVINST devInst) {
        DEVINST childDevInst;
        CONFIGRET cr = CM_Get_Child(&childDevInst, devInst, 0);
        while (cr == CR_SUCCESS) {
            print_details(childDevInst);
            recurse(childDevInst);

            DEVINST siblingDevInst;
            cr = CM_Get_Sibling(&siblingDevInst, childDevInst, 0);
            childDevInst = siblingDevInst;
        }
    }
}

void cfgmgr32_child_recurse() {
    DEVINST devInst;
    CONFIGRET cr = CM_Locate_DevNode(&devInst, nullptr, CM_LOCATE_DEVNODE_PHANTOM);
    if (cr != CR_SUCCESS) {
        std::wcerr << L"CM_Locate_DevNode(&devInst, nullptr, CM_LOCATE_DEVNODE_PHANTOM) returned " << cr << std::endl;
        return;
    }

    recurse(devInst);
}
